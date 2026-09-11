using Bms.BuildingBlocks.Abstractions.Security;
using Bms.BuildingBlocks.Core.Search;
using Bms.Store.Application.Dtos;
using Bms.Store.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Bms.Store.Application.Services;

/// <summary>
/// 全局搜索应用服务实现（门店业务数据）
/// 六分组（顾客/订单/商品/项目卡/储值/预约）查询，仅返回有命中的分组；
/// 租户隔离由 EF 全局过滤器与显式条件双保险，门店隔离按现有 AppService 惯例手动过滤。
/// 说明：服务端暂无按权限码过滤的通用设施，一期分组为「登录即可见」，
/// 数据可见性由租户/门店隔离兜底，分组展示由前端权限码控制。
/// </summary>
public class GlobalSearchAppService : IGlobalSearchAppService
{
    private readonly StoreDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public GlobalSearchAppService(StoreDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    /// <inheritdoc />
    public async Task<ApiResponseDto<List<SearchResultGroupDto>>> SearchAsync(GlobalSearchRequestDto request)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<List<SearchResultGroupDto>>.Fail("登录状态异常，请重新登录", 401);

        var kw = request.Keyword?.Trim() ?? string.Empty;
        if (kw.Length == 0 || kw.Length > 50)
            return ApiResponseDto<List<SearchResultGroupDto>>.Fail("搜索关键字无效");
        var limit = Math.Clamp(request.Limit, 1, 10);
        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId ?? 0;

        // 六个分组顺序执行：同一 DbContext 实例不允许并发查询（会抛 InvalidOperationException），
        // 且各查询均为小结果集 top-N，串行总延迟可忽略
        var groups = new List<SearchResultGroupDto>();
        void Add(string group, List<SearchResultItemDto> items)
        {
            if (items.Count > 0)
                groups.Add(new SearchResultGroupDto { Group = group, Items = items });
        }

        Add("顾客", await SearchCustomersAsync(kw, limit, tenantId, storeId));
        Add("订单", await SearchOrdersAsync(kw, limit, tenantId, storeId));
        Add("商品", await SearchProductsAsync(kw, limit, tenantId));
        Add("项目卡", await SearchTreatmentCardSalesAsync(kw, limit, tenantId, storeId));
        Add("储值", await SearchStoredValueAccountsAsync(kw, limit, tenantId, storeId));
        Add("预约", await SearchAppointmentsAsync(kw, limit, tenantId, storeId));

        return ApiResponseDto<List<SearchResultGroupDto>>.Ok(groups);
    }

    /// <summary>
    /// 顾客分组：按姓名/手机号模糊匹配
    /// </summary>
    private async Task<List<SearchResultItemDto>> SearchCustomersAsync(string kw, int limit, long tenantId, long storeId)
    {
        return await _dbContext.Customers
            .Where(c => !c.IsDeleted && c.TenantId == tenantId && c.StoreId == storeId)
            .Where(c => c.Name.Contains(kw) || c.Phone.Contains(kw))
            .OrderBy(c => c.Name)
            .Take(limit)
            .Select(c => new SearchResultItemDto { Title = c.Name, Subtitle = c.Phone })
            .ToListAsync();
    }

    /// <summary>
    /// 订单分组：按订单号/关联顾客姓名模糊匹配（Order 无冗余顾客姓名字段，经导航属性联查）
    /// </summary>
    private async Task<List<SearchResultItemDto>> SearchOrdersAsync(string kw, int limit, long tenantId, long storeId)
    {
        // 金额/时间格式化无法翻译为 SQL，先投影原始字段再一次往返后内存组装
        var rows = await _dbContext.Orders
            .Where(o => o.TenantId == tenantId && o.StoreId == storeId)
            .Where(o => o.OrderNo.Contains(kw) || (o.Customer != null && o.Customer.Name.Contains(kw)))
            .OrderByDescending(o => o.OrderTime)
            .Take(limit)
            .Select(o => new { o.OrderNo, CustomerName = o.Customer != null ? o.Customer.Name : null, o.PaidAmount })
            .ToListAsync();

        return rows.Select(o => new SearchResultItemDto
        {
            Title = o.OrderNo,
            Subtitle = $"{o.CustomerName ?? "散客"} · ¥{o.PaidAmount:F2}"
        }).ToList();
    }

    /// <summary>
    /// 商品分组：按名称/编码模糊匹配（商品主档为租户级数据，无门店维度）
    /// </summary>
    private async Task<List<SearchResultItemDto>> SearchProductsAsync(string kw, int limit, long tenantId)
    {
        return await _dbContext.ProductMasters
            .Where(p => !p.IsDeleted && p.TenantId == tenantId)
            .Where(p => p.Name.Contains(kw) || p.Code.Contains(kw))
            .OrderBy(p => p.Name)
            .Take(limit)
            .Select(p => new SearchResultItemDto { Title = p.Name, Subtitle = p.Code })
            .ToListAsync();
    }

    /// <summary>
    /// 项目卡分组：按销售单号/关联顾客姓名与手机号模糊匹配
    /// </summary>
    private async Task<List<SearchResultItemDto>> SearchTreatmentCardSalesAsync(string kw, int limit, long tenantId, long storeId)
    {
        var rows = await _dbContext.TreatmentCardSales
            .Where(s => !s.IsDeleted && s.TenantId == tenantId && s.StoreId == storeId)
            .Where(s => (s.SaleNo != null && s.SaleNo.Contains(kw)) ||
                        s.Customer.Name.Contains(kw) || s.Customer.Phone.Contains(kw))
            .OrderByDescending(s => s.PurchaseDate)
            .Take(limit)
            .Select(s => new { s.SaleNo, CustomerName = s.Customer.Name, s.PurchaseDate })
            .ToListAsync();

        return rows.Select(s => new SearchResultItemDto
        {
            Title = s.SaleNo ?? string.Empty,
            Subtitle = $"{s.CustomerName} · {s.PurchaseDate:yyyy-MM-dd}"
        }).ToList();
    }

    /// <summary>
    /// 储值分组：按关联顾客姓名与手机号模糊匹配
    /// </summary>
    private async Task<List<SearchResultItemDto>> SearchStoredValueAccountsAsync(string kw, int limit, long tenantId, long storeId)
    {
        // 余额格式化无法翻译为 SQL，先投影原始字段再一次往返后内存组装
        var rows = await _dbContext.StoredValueAccounts
            .Where(a => !a.IsDeleted && a.TenantId == tenantId && a.StoreId == storeId)
            .Where(a => a.Customer.Name.Contains(kw) || a.Customer.Phone.Contains(kw))
            .OrderBy(a => a.Customer.Name)
            .Take(limit)
            .Select(a => new { CustomerName = a.Customer.Name, a.Balance })
            .ToListAsync();

        return rows.Select(a => new SearchResultItemDto
        {
            Title = a.CustomerName,
            Subtitle = $"余额 ¥{a.Balance:F2}"
        }).ToList();
    }

    /// <summary>
    /// 预约分组：按预约单号/顾客姓名模糊匹配（Appointment 冗余了顾客姓名字段，无需联查）
    /// </summary>
    private async Task<List<SearchResultItemDto>> SearchAppointmentsAsync(string kw, int limit, long tenantId, long storeId)
    {
        // 预约时间格式化无法翻译为 SQL，先投影原始字段再一次往返后内存组装
        var rows = await _dbContext.Appointments
            .Where(a => a.TenantId == tenantId && a.StoreId == storeId)
            .Where(a => a.AppointmentNo.Contains(kw) || a.CustomerName.Contains(kw))
            .OrderByDescending(a => a.StartTime)
            .Take(limit)
            .Select(a => new { a.AppointmentNo, a.CustomerName, a.StartTime })
            .ToListAsync();

        return rows.Select(a => new SearchResultItemDto
        {
            Title = a.AppointmentNo,
            Subtitle = $"{a.CustomerName} · {a.StartTime:yyyy-MM-dd HH:mm}"
        }).ToList();
    }
}
