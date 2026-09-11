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
/// 预警提醒聚合 InventoryAlert/InventoryBatch/TreatmentCardSale/Customer 四类来源
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
    /// 营收口径复用 SummarizeCoreAsync，含项目卡核销折算（P-DASH-01）
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
                && a.StoreId == ctx.StoreId
                && a.StartTime.Date == today)
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
    /// 默认排除 ProductType=5（项目卡核销，非销售行为）（P-DS-05）
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
            // 默认排除 ProductType=5（项目卡核销，非销售行为）
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
    /// 获取营收构成（按 ProductType 分组聚合 ProductSalesStat）
    /// </summary>
    public async Task<ApiResponseDto<List<RevenueCompositionItemDto>>> GetRevenueCompositionAsync(MonthlyTrendQueryDto query)
    {
        var ctx = ResolveTenantStore();
        if (ctx.Error != null)
            return ApiResponseDto<List<RevenueCompositionItemDto>>.Fail(ctx.Error, ctx.Code);

        var statMonth = $"{query.Year}-{query.Month:D2}";

        // 商品类型名称映射（4=项目卡购买不写统计表，理论上无数据，保留映射以兼容历史/特殊数据）
        var typeNames = new Dictionary<int, string>
        {
            { 1, "零售" },
            { 2, "服务" },
            { 3, "耗材" },
            { 4, "项目卡" },
            { 5, "项目卡核销" }
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
    /// 获取首页预警提醒（聚合库存预警/批次临期/项目卡到期/客户生日，按紧急度排序取前 10 条）
    /// 库存预警仅取低库存/积压（效期预警由批次临期覆盖，避免重复提醒）；
    /// 项目卡到期为客户视角跨店口径（不按 StoreId 过滤），与到期列表页一致；
    /// 客户生日窗口与 BirthdayReminderService 一致（未来 3 天含当天）
    /// </summary>
    public async Task<ApiResponseDto<List<DashboardAlertDto>>> GetDashboardAlertsAsync()
    {
        var ctx = ResolveTenantStore();
        if (ctx.Error != null)
            return ApiResponseDto<List<DashboardAlertDto>>.Fail(ctx.Error, ctx.Code);

        var now = DateTime.Now;
        var today = now.Date;

        // 1. 库存预警：未处理的低库存(1)/积压(3)，最新优先
        var inventoryAlerts = await _dbContext.InventoryAlerts
            .Where(a => a.TenantId == ctx.TenantId && a.StoreId == ctx.StoreId
                && !a.IsProcessed && (a.AlertType == 1 || a.AlertType == 3))
            .OrderByDescending(a => a.CreatedTime)
            .Take(5)
            .Select(a => new { a.AlertType, a.CurrentQuantity, a.CreatedTime, ProductName = a.Product!.Master!.Name })
            .ToListAsync();

        var inventoryItems = inventoryAlerts.Select(a =>
        {
            var isShortage = a.AlertType == 1;
            return new DashboardAlertDto
            {
                Level = isShortage ? "danger" : "warning",
                Title = isShortage ? "商品库存不足" : "商品库存积压",
                Desc = isShortage
                    ? $"\"{a.ProductName}\" 库存仅剩 {a.CurrentQuantity:0.##} 件"
                    : $"\"{a.ProductName}\" 库存积压 {a.CurrentQuantity:0.##} 件",
                TagText = isShortage ? "紧急" : "积压",
                Time = FormatAlertTime(a.CreatedTime, now)
            };
        });

        // 2. 批次临期：30 天内到期且仍有库存的批次，临期最早优先
        var expiringBatches = await _dbContext.InventoryBatches
            .Where(b => b.TenantId == ctx.TenantId && b.StoreId == ctx.StoreId
                && b.Status == 1 && b.Quantity > 0
                && b.ExpirationDate >= today && b.ExpirationDate < today.AddDays(30))
            .OrderBy(b => b.ExpirationDate)
            .Take(5)
            .Select(b => new { b.BatchNo, b.Quantity, b.ExpirationDate, ProductName = b.Product!.Master!.Name })
            .ToListAsync();

        var batchItems = expiringBatches.Select(b =>
        {
            var remainingDays = (b.ExpirationDate!.Value.Date - today).Days;
            return new DashboardAlertDto
            {
                Level = "danger",
                Title = "商品批次临期",
                Desc = $"\"{b.ProductName}\" 批次 {b.BatchNo} 剩余 {b.Quantity:0.##} 件，{remainingDays} 天后过期",
                TagText = "临期",
                Time = $"{remainingDays} 天"
            };
        });

        // 3. 项目卡到期：30 天内到期或已过期的有效卡，客户视角不按 StoreId 过滤（跨店权益规范 4.1/5.4 节）
        var cardSales = await _dbContext.TreatmentCardSales
            .Where(s => s.TenantId == ctx.TenantId
                && !s.IsDeleted && s.Status == 1
                && s.ExpiryDate <= now.AddDays(30))
            .OrderBy(s => s.ExpiryDate)
            .Take(5)
            .Select(s => new { s.ExpiryDate, s.RemainingTimes, CardName = s.Card!.Name, CustomerName = s.Customer!.Name })
            .ToListAsync();

        var cardItems = cardSales.Select(s =>
        {
            var remainingDays = (s.ExpiryDate.Date - today).Days;
            var expired = remainingDays < 0;
            return new DashboardAlertDto
            {
                Level = expired ? "danger" : "warning",
                Title = expired ? "项目卡已到期" : "项目卡即将到期",
                Desc = expired
                    ? $"客户\"{s.CustomerName}\" 的{s.CardName}已过期 {-remainingDays} 天，剩余 {s.RemainingTimes} 次"
                    : $"客户\"{s.CustomerName}\" 的{s.CardName}剩余 {s.RemainingTimes} 次，{remainingDays} 天后到期",
                TagText = expired ? "到期" : "提醒",
                Time = expired ? $"过期 {-remainingDays} 天" : $"剩余 {remainingDays} 天"
            };
        });

        // 4. 客户生日：未来 3 天含当天（按月日匹配），跨年窗口由 next<today 补一年处理
        var d0 = today;
        var d1 = today.AddDays(1);
        var d2 = today.AddDays(2);
        var birthdayCustomers = await _dbContext.Customers
            .Where(c => c.TenantId == ctx.TenantId && c.StoreId == ctx.StoreId && c.Birthday != null
                && ((c.Birthday.Value.Month == d0.Month && c.Birthday.Value.Day == d0.Day)
                 || (c.Birthday.Value.Month == d1.Month && c.Birthday.Value.Day == d1.Day)
                 || (c.Birthday.Value.Month == d2.Month && c.Birthday.Value.Day == d2.Day)))
            .Select(c => new { c.Name, Birthday = c.Birthday!.Value })
            .ToListAsync();

        var birthdayItems = birthdayCustomers
            .Select(c =>
            {
                var next = new DateTime(today.Year, c.Birthday.Month, c.Birthday.Day);
                if (next < today)
                    next = next.AddYears(1);
                return new { c.Name, Days = (next - today).Days };
            })
            .OrderBy(x => x.Days)
            .Take(5)
            .Select(x => new DashboardAlertDto
            {
                Level = "warning",
                Title = "客户生日提醒",
                Desc = x.Days == 0
                    ? $"客户\"{x.Name}\" 今天生日，可发送关怀问候"
                    : $"客户\"{x.Name}\" {x.Days} 天后生日，可发送关怀问候",
                TagText = "关怀",
                Time = x.Days == 0 ? "今天" : $"{x.Days} 天后"
            });

        // 合并：紧急(danger)优先，同级保持各类紧迫顺序，最多返回 10 条
        var result = inventoryItems
            .Concat(batchItems)
            .Concat(cardItems)
            .Concat(birthdayItems)
            .OrderBy(x => x.Level == "danger" ? 0 : 1)
            .Take(10)
            .ToList();

        return ApiResponseDto<List<DashboardAlertDto>>.Ok(result);
    }

    /// <summary>
    /// 预警时间展示：当天显示时刻（HH:mm），更早显示日期（MM-dd）
    /// </summary>
    private static string FormatAlertTime(DateTime createdTime, DateTime now)
    {
        return createdTime.Date == now.Date
            ? createdTime.ToString("HH:mm")
            : createdTime.ToString("MM-dd");
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
                && a.StoreId == storeId
                && a.StartTime.Date == today)
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
            return new TenantStoreContext(0, 0, "登录状态异常，请重新登录", 401);
        if (!_currentUser.StoreId.HasValue)
            return new TenantStoreContext(0, 0, "请选择门店", 400);
        return new TenantStoreContext(_currentUser.TenantId.Value, _currentUser.StoreId.Value, null, 0);
    }

    private record TenantStoreContext(long TenantId, long StoreId, string? Error, int Code);
}
