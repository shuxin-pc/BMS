using Mapster;
using Microsoft.EntityFrameworkCore;
using Bms.BuildingBlocks.Abstractions.Security;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Dashboard;
using Bms.Store.Application.Dtos.Statistics;
using Bms.Store.Infrastructure;

namespace Bms.Store.Application.Services;

/// <summary>
/// 首页看板聚合查询服务实现
/// 今日数据实时聚合 Order/StoredValueLog/Appointment/InventoryAlert 表
/// 月度趋势查询 DailyStat 表（历史已日结数据 + 今日实时追加）
/// 热门商品和营收构成查询 ProductSalesStat 表
/// 营收口径直接复用 DailySettlementAppService.SummarizeCoreAsync，确保 Dashboard 与日结数据一致（P-DASH-01）
/// </summary>
public class DashboardAppService : IDashboardAppService
{
    private readonly StoreDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDailySettlementAppService _dailySettlementAppService;

    public DashboardAppService(
        StoreDbContext dbContext,
        ICurrentUser currentUser,
        IDailySettlementAppService dailySettlementAppService)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _dailySettlementAppService = dailySettlementAppService;
    }

    /// <summary>
    /// 获取今日核心指标（实时聚合）
    /// 营收口径复用 SummarizeCoreAsync，含疗程卡核销折算（P-DASH-01）
    /// </summary>
    public async Task<ApiResponseDto<DashboardSummaryDto>> GetDashboardSummaryAsync()
    {
        var ctx = ResolveTenantStore();
        if (ctx.Error != null)
            return ApiResponseDto<DashboardSummaryDto>.Fail(ctx.Error, ctx.Code);

        var today = DateTime.Today;

        // 复用日结聚合口径，确保 Dashboard 与日结数据一致（P-DASH-01）
        var data = await _dailySettlementAppService.SummarizeCoreAsync(ctx.TenantId, ctx.StoreId, today);

        // 今日消费客户数（去重）
        var todayConsumeCustomerCount = await _dbContext.Orders
            .Where(o => o.TenantId == ctx.TenantId && o.StoreId == ctx.StoreId
                && o.OrderTime >= today && o.OrderTime < today.AddDays(1)
                && (o.Status == 2 || o.Status == 3)
                && o.CustomerId.HasValue)
            .Select(o => o.CustomerId.Value)
            .Distinct()
            .CountAsync();

        // 今日新客数
        var todayNewCustomerCount = await _dbContext.Customers
            .Where(c => c.TenantId == ctx.TenantId
                && c.CreatedTime >= today && c.CreatedTime < today.AddDays(1))
            .CountAsync();

        // 今日预约数
        var todayAppointmentCount = await _dbContext.Appointments
            .Where(a => a.TenantId == ctx.TenantId
                && a.AppointmentDate == today)
            .CountAsync();

        // 库存预警数（未处理）
        var inventoryAlertCount = await _dbContext.InventoryAlerts
            .Where(a => a.TenantId == ctx.TenantId
                && a.StoreId == ctx.StoreId
                && !a.IsProcessed)
            .CountAsync();

        var dto = new DashboardSummaryDto
        {
            TodayRevenue = data.TotalRevenue,
            TodayCashRevenue = data.CashRevenue,
            TodayStoredValueRevenue = data.StoredValueRevenue,
            TodayPointsDeductAmount = data.PointsDeductAmount,
            TodayTreatmentCardVerifyAmount = data.TreatmentCardVerifyAmount,
            TodayRefundAmount = data.TotalRefund,
            TodayCashRefundAmount = data.CashRefundAmount,
            TodayOrderCount = data.OrderCount,
            TodayGrossProfit = data.TotalGrossProfit,
            TodayConsumeCustomerCount = todayConsumeCustomerCount,
            TodayNewCustomerCount = todayNewCustomerCount,
            TodayAppointmentCount = todayAppointmentCount,
            InventoryAlertCount = inventoryAlertCount
        };

        return ApiResponseDto<DashboardSummaryDto>.Ok(dto);
    }

    /// <summary>
    /// 获取月度营收趋势（DailyStat 历史 + 今日实时追加）
    /// </summary>
    public async Task<ApiResponseDto<List<DailyStatDto>>> GetMonthlyTrendAsync(MonthlyTrendQueryDto query)
    {
        var ctx = ResolveTenantStore();
        if (ctx.Error != null)
            return ApiResponseDto<List<DailyStatDto>>.Fail(ctx.Error, ctx.Code);

        var monthStart = new DateTime(query.Year, query.Month, 1);
        var monthEnd = monthStart.AddMonths(1);

        // 查询该月已日结的 DailyStat 记录
        var stats = await _dbContext.DailyStats
            .Where(s => s.TenantId == ctx.TenantId && s.StoreId == ctx.StoreId
                && s.StatDate >= monthStart && s.StatDate < monthEnd)
            .OrderBy(s => s.StatDate)
            .ToListAsync();

        var result = stats.Adapt<List<DailyStatDto>>();

        // 今日在该月且尚未有 DailyStat 记录时，实时聚合今日数据追加
        var today = DateTime.Today;
        if (today >= monthStart && today < monthEnd)
        {
            var hasToday = stats.Any(s => s.StatDate.Date == today);
            if (!hasToday)
            {
                var todayData = await BuildTodayStatAsync(ctx.TenantId, ctx.StoreId, today);
                result.Add(todayData);
            }
        }

        return ApiResponseDto<List<DailyStatDto>>.Ok(result);
    }

    /// <summary>
    /// 获取热门商品 TOP N（查询 ProductSalesStat 表，按 ProductId 聚合）
    /// 默认排除 ProductType=5（疗程卡核销，非销售行为）（P-DS-05）
    /// </summary>
    public async Task<ApiResponseDto<List<ProductSalesStatDto>>> GetTopProductsAsync(TopProductsQueryDto query)
    {
        var ctx = ResolveTenantStore();
        if (ctx.Error != null)
            return ApiResponseDto<List<ProductSalesStatDto>>.Fail(ctx.Error, ctx.Code);

        var statMonth = $"{query.Year}-{query.Month:D2}";
        var top = query.Top > 0 ? query.Top : 5;

        var queryable = _dbContext.ProductSalesStats
            .Where(s => s.TenantId == ctx.TenantId && s.StoreId == ctx.StoreId && s.StatMonth == statMonth);

        if (query.ProductTypes != null && query.ProductTypes.Count > 0)
            queryable = queryable.Where(s => query.ProductTypes.Contains(s.ProductType));
        else
            // 默认排除 ProductType=5（疗程卡核销，非销售行为）
            queryable = queryable.Where(s => s.ProductType != 5);

        // 按 ProductId 分组聚合
        var groupedQuery = queryable
            .GroupBy(s => new { s.ProductId, s.ProductName, s.ProductType })
            .Select(g => new ProductSalesStatDto
            {
                ProductId = g.Key.ProductId,
                ProductName = g.Key.ProductName,
                ProductType = g.Key.ProductType,
                SalesCount = g.Sum(s => s.SalesCount),
                SalesAmount = g.Sum(s => s.SalesAmount),
                StatMonth = statMonth
            });

        // SortBy: count=按次数排序，其他（含默认 amount）=按金额排序
        if (string.Equals(query.SortBy, "count", StringComparison.OrdinalIgnoreCase))
            groupedQuery = groupedQuery.OrderByDescending(x => x.SalesCount);
        else
            groupedQuery = groupedQuery.OrderByDescending(x => x.SalesAmount);

        var grouped = await groupedQuery
            .Take(top)
            .ToListAsync();

        return ApiResponseDto<List<ProductSalesStatDto>>.Ok(grouped);
    }

    /// <summary>
    /// 获取热门商品 TOP N（仅零售+耗材，ProductType=1,3）（P-DS-05）
    /// </summary>
    public async Task<ApiResponseDto<List<ProductSalesStatDto>>> GetTopProductsBySalesAsync(TopProductsQueryDto query)
    {
        query.ProductTypes = new List<int> { 1, 3 };
        return await GetTopProductsAsync(query);
    }

    /// <summary>
    /// 获取热门服务 TOP N（仅服务+疗程卡购买，ProductType=2,4）（P-DS-05）
    /// 疗程卡核销（ProductType=5）不计入排行
    /// </summary>
    public async Task<ApiResponseDto<List<ProductSalesStatDto>>> GetTopServicesBySalesAsync(TopProductsQueryDto query)
    {
        query.ProductTypes = new List<int> { 2, 4 };
        return await GetTopProductsAsync(query);
    }

    /// <summary>
    /// 获取营收构成（按 ProductType 分组聚合 ProductSalesStat）
    /// </summary>
    public async Task<ApiResponseDto<List<RevenueCompositionItemDto>>> GetRevenueCompositionAsync(MonthlyTrendQueryDto query)
    {
        var ctx = ResolveTenantStore();
        if (ctx.Error != null)
            return ApiResponseDto<List<RevenueCompositionItemDto>>.Fail(ctx.Error, ctx.Code);

        var statMonth = $"{query.Year}-{query.Month:D2}";

        // 商品类型名称映射
        var typeNames = new Dictionary<int, string>
        {
            { 1, "零售" },
            { 2, "服务" },
            { 3, "耗材" },
            { 4, "疗程卡" }
        };

        var grouped = await _dbContext.ProductSalesStats
            .Where(s => s.TenantId == ctx.TenantId && s.StoreId == ctx.StoreId && s.StatMonth == statMonth)
            .GroupBy(s => s.ProductType)
            .Select(g => new
            {
                ProductType = g.Key,
                TotalAmount = g.Sum(s => s.SalesAmount)
            })
            .ToListAsync();

        var result = grouped
            .Select(g => new RevenueCompositionItemDto
            {
                Name = typeNames.TryGetValue(g.ProductType, out var name) ? name : $"类型{g.ProductType}",
                Value = g.TotalAmount
            })
            .Where(x => x.Value > 0)
            .OrderByDescending(x => x.Value)
            .ToList();

        return ApiResponseDto<List<RevenueCompositionItemDto>>.Ok(result);
    }

    /// <summary>
    /// 实时聚合今日统计数据（用于月度趋势追加末尾）
    /// 营收口径复用 SummarizeCoreAsync，确保与日结数据一致（P-DASH-01）
    /// </summary>
    private async Task<DailyStatDto> BuildTodayStatAsync(long tenantId, long storeId, DateTime today)
    {
        var dateStart = today;
        var dateEnd = today.AddDays(1);

        // 复用日结聚合口径，确保 Dashboard 与日结数据一致（P-DASH-01）
        var data = await _dailySettlementAppService.SummarizeCoreAsync(tenantId, storeId, today);

        // 消费客户数（去重）
        var consumeCustomerCount = await _dbContext.Orders
            .Where(o => o.TenantId == tenantId && o.StoreId == storeId
                && o.OrderTime >= dateStart && o.OrderTime < dateEnd
                && (o.Status == 2 || o.Status == 3)
                && o.CustomerId.HasValue)
            .Select(o => o.CustomerId.Value)
            .Distinct()
            .CountAsync();

        var newCustomerCount = await _dbContext.Customers
            .Where(c => c.TenantId == tenantId
                && c.CreatedTime >= dateStart && c.CreatedTime < dateEnd)
            .CountAsync();

        var appointmentCount = await _dbContext.Appointments
            .Where(a => a.TenantId == tenantId
                && a.AppointmentDate == today)
            .CountAsync();

        var inventoryAlertCount = await _dbContext.InventoryAlerts
            .Where(a => a.TenantId == tenantId && a.StoreId == storeId && !a.IsProcessed)
            .CountAsync();

        return new DailyStatDto
        {
            StatDate = today,
            Revenue = data.TotalRevenue,
            CashRevenue = data.CashRevenue,
            StoredValueRevenue = data.StoredValueRevenue,
            PointsDeductAmount = data.PointsDeductAmount,
            Cost = data.TotalCost,
            GrossProfit = data.TotalGrossProfit,
            OrderCount = data.OrderCount,
            RefundAmount = data.TotalRefund,
            CashRefundAmount = data.CashRefundAmount,
            StoredValueRecharge = data.TotalStoredValueRecharge,
            StoredValueConsume = data.TotalStoredValueConsume,
            ConsumeCustomerCount = consumeCustomerCount,
            NewCustomerCount = newCustomerCount,
            AppointmentCount = appointmentCount,
            InventoryAlertCount = inventoryAlertCount,
            TreatmentCardVerifyAmount = data.TreatmentCardVerifyAmount
        };
    }

    /// <summary>
    /// 解析当前租户与门店
    /// </summary>
    private TenantStoreContext ResolveTenantStore()
    {
        if (!_currentUser.TenantId.HasValue)
            return new TenantStoreContext(0, 0, "无法确定当前租户", 401);
        if (!_currentUser.StoreId.HasValue)
            return new TenantStoreContext(0, 0, "请选择门店", 400);
        return new TenantStoreContext(_currentUser.TenantId.Value, _currentUser.StoreId.Value, null, 0);
    }

    private record TenantStoreContext(long TenantId, long StoreId, string? Error, int Code);
}
