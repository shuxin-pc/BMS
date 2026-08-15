using Mapster;
using Microsoft.EntityFrameworkCore;
using Bms.BuildingBlocks.Abstractions.Security;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Products;
using Bms.Store.Application.Dtos.SampleGifts;
using ProductEntity = Bms.Store.Domain.Entities.Product;
using InventoryEntity = Bms.Store.Domain.Entities.Inventory;
using Bms.Store.Infrastructure;

namespace Bms.Store.Application.Services;

/// <summary>
/// 样品/赠品档案应用服务实现
/// 样品/赠品即 Product 表中 Type=4（样品）或 Type=5（赠品）的记录
/// 提供库存查询、统计报表功能（档案管理已统一至 ProductMaster/Product 体系）
/// </summary>
public class SampleGiftAppService : ISampleGiftAppService
{
    private readonly StoreDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public SampleGiftAppService(StoreDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    // ========== 档案管理 CRUD ==========

    /// <summary>
    /// 获取样品/赠品分页列表（Type=4或5）
    /// </summary>
    public async Task<ApiResponseDto<PagedResponseDto<ProductDto>>> GetPagedListAsync(SampleGiftQueryDto query)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<PagedResponseDto<ProductDto>>.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId ?? 0;
        var queryable = _dbContext.Products
            .Include(p => p.Master).ThenInclude(m => m.Category)
            .Where(p => !p.IsDeleted && p.TenantId == tenantId && p.StoreId == storeId && (p.Master.Type == 4 || p.Master.Type == 5));

        if (!string.IsNullOrWhiteSpace(query.Name))
            queryable = queryable.Where(p => p.Master.Name.Contains(query.Name));
        if (!string.IsNullOrWhiteSpace(query.Code))
            queryable = queryable.Where(p => p.Master.Code.Contains(query.Code));
        if (query.Type.HasValue)
            queryable = queryable.Where(p => p.Master.Type == query.Type.Value);
        if (query.Status.HasValue)
            queryable = queryable.Where(p => p.Status == query.Status.Value);

        var total = await queryable.CountAsync();
        var items = await queryable
            .OrderByDescending(p => p.CreatedTime)
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var dtoList = items.Adapt<List<ProductDto>>();
        // Adapt 不会自动扁平映射 Master.Name -> Name，手动从 Master 导航填充
        foreach (var dto in dtoList)
        {
            var entity = items.FirstOrDefault(e => e.Id == dto.Id);
            FillMasterFields(dto, entity);
        }
        var result = new PagedResponseDto<ProductDto>
        {
            List = dtoList,
            Total = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
        return ApiResponseDto<PagedResponseDto<ProductDto>>.Ok(result);
    }

    // ========== 统计报表 ==========

    /// <summary>
    /// 获取样品/赠品统计报表分页列表
    /// 聚合 SampleGiftReceive（领用）和 InventoryLogs（出库，SourceType 9/10）数量
    /// </summary>
    public async Task<ApiResponseDto<PagedResponseDto<SampleReportDto>>> GetReportListAsync(SampleReportQueryDto query)
    {
        if (!_currentUser.TenantId.HasValue || !_currentUser.StoreId.HasValue)
            return ApiResponseDto<PagedResponseDto<SampleReportDto>>.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId.Value;
        var queryable = _dbContext.Products
            .Include(p => p.Master)
            .Where(p => !p.IsDeleted && p.TenantId == tenantId && p.StoreId == storeId && (p.Master.Type == 4 || p.Master.Type == 5));

        if (!string.IsNullOrWhiteSpace(query.Name))
            queryable = queryable.Where(p => p.Master.Name.Contains(query.Name));
        if (query.Type.HasValue)
            queryable = queryable.Where(p => p.Master.Type == query.Type.Value);

        var total = await queryable.CountAsync();
        var products = await queryable
            .OrderByDescending(p => p.CreatedTime)
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var productIds = products.Select(p => p.Id).ToList();

        // 批量查询领用数量
        var receiveQuery = _dbContext.SampleGiftReceives
            .Where(r => r.TenantId == tenantId && r.StoreId == storeId && productIds.Contains(r.ProductId));
        if (query.StartDate.HasValue)
            receiveQuery = receiveQuery.Where(r => r.ReceiveTime >= query.StartDate.Value);
        if (query.EndDate.HasValue)
            receiveQuery = receiveQuery.Where(r => r.ReceiveTime <= query.EndDate.Value);
        var receiveCounts = await receiveQuery
            .GroupBy(r => r.ProductId)
            .Select(g => new { ProductId = g.Key, Total = g.Sum(r => r.Quantity) })
            .ToDictionaryAsync(x => x.ProductId, x => x.Total);

        // 批量查询出库数量（R5：数据源从 SampleGiftOuts 迁移到 InventoryLogs）
        // SourceType 9=SampleReceiveOutbound 样品领用出库，10=GiftOutbound 赠品活动出库
        // InventoryLog.Quantity 出库为负数，求和后取反还原为正数
        var outQuery = _dbContext.InventoryLogs
            .Where(l => l.TenantId == tenantId && l.StoreId == storeId
                && l.Type == 2 && (l.SourceType == 9 || l.SourceType == 10)
                && productIds.Contains(l.ProductId));
        if (query.StartDate.HasValue)
            outQuery = outQuery.Where(l => l.CreatedTime >= query.StartDate.Value);
        if (query.EndDate.HasValue)
            outQuery = outQuery.Where(l => l.CreatedTime <= query.EndDate.Value);
        var outCounts = await outQuery
            .GroupBy(l => l.ProductId)
            .Select(g => new { ProductId = g.Key, Total = -g.Sum(l => l.Quantity) })
            .ToDictionaryAsync(x => x.ProductId, x => x.Total);

        // 批量查询当前库存
        var stockDict = await _dbContext.Inventories
            .Where(i => i.TenantId == tenantId && i.StoreId == storeId && productIds.Contains(i.ProductId))
            .GroupBy(i => i.ProductId)
            .Select(g => new { ProductId = g.Key, Total = g.Sum(i => i.Quantity) })
            .ToDictionaryAsync(x => x.ProductId, x => x.Total);

        // 组装报表 DTO
        var dtoList = products.Select(p =>
        {
            var receiveCount = receiveCounts.GetValueOrDefault(p.Id, 0);
            var outboundCount = outCounts.GetValueOrDefault(p.Id, 0);
            var totalIssued = receiveCount + outboundCount;
            var currentStock = stockDict.GetValueOrDefault(p.Id, 0);
            return new SampleReportDto
            {
                Id = p.Id,
                Name = p.Master?.Name ?? string.Empty,
                Type = p.Master?.Type ?? 0,
                ReceiveCount = receiveCount,
                OutboundCount = outboundCount,
                TotalIssued = totalIssued,
                CurrentStock = currentStock,
                ReceiveRatio = totalIssued == 0 ? 0 : Math.Round(receiveCount * 100 / totalIssued, 2)
            };
        }).ToList();

        var result = new PagedResponseDto<SampleReportDto>
        {
            List = dtoList,
            Total = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
        return ApiResponseDto<PagedResponseDto<SampleReportDto>>.Ok(result);
    }

    /// <summary>
    /// 获取样品/赠品按活动维度统计报表分页列表（P-SG-04）
    /// R5：数据源从 SampleGiftOut 迁移到 InventoryLogs（SourceType 9=样品领用出库/10=赠品活动出库）
    /// 仅统计 ActivityId 有值的记录，按 ActivityId + ProductId 聚合
    /// 分页在内存中处理（GroupBy 后 EF Core 对 Skip/Take 翻译有限制）
    /// </summary>
    public async Task<ApiResponseDto<PagedResponseDto<SampleActivityReportDto>>> GetReportByActivityAsync(SampleActivityReportQueryDto query)
    {
        if (!_currentUser.TenantId.HasValue || !_currentUser.StoreId.HasValue)
            return ApiResponseDto<PagedResponseDto<SampleActivityReportDto>>.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId.Value;

        // R5：数据源从 SampleGiftOuts 迁移到 InventoryLogs
        // WHERE Type=2(出库) AND SourceType IN (9,10) AND ActivityId IS NOT NULL
        // JOIN Products 填充商品名称/成本价
        var queryable = from l in _dbContext.InventoryLogs
                        join p in _dbContext.Products on l.ProductId equals p.Id
                        where l.TenantId == tenantId && l.StoreId == storeId
                            && l.Type == 2 && (l.SourceType == 9 || l.SourceType == 10)
                            && l.ActivityId.HasValue && !p.IsDeleted
                        select new { l, p };

        if (query.ActivityId.HasValue)
            queryable = queryable.Where(x => x.l.ActivityId == query.ActivityId.Value);
        if (query.ProductId.HasValue)
            queryable = queryable.Where(x => x.l.ProductId == query.ProductId.Value);
        if (query.StartDate.HasValue)
            queryable = queryable.Where(x => x.l.CreatedTime >= query.StartDate.Value);
        if (query.EndDate.HasValue)
            queryable = queryable.Where(x => x.l.CreatedTime <= query.EndDate.Value);

        // 按 ActivityId + ProductId 分组聚合（EF Core 可翻译 GroupBy + Sum）
        // InventoryLog.Quantity 出库为负数，求和后取反还原为正数
        // CostPrice 为 decimal? 类型，求和前需处理 null（按 0 计入）
        var grouped = await queryable
            .GroupBy(x => new { x.l.ActivityId, x.l.ProductId })
            .Select(g => new SampleActivityReportDto
            {
                ActivityId = g.Key.ActivityId,
                ProductId = g.Key.ProductId,
                ProductName = g.Select(x => x.p.Master.Name).FirstOrDefault() ?? string.Empty,
                OutboundCount = -g.Sum(x => x.l.Quantity),
                TotalValue = -g.Sum(x => x.l.Quantity * (x.p.CostPrice ?? 0m)),
                OutboundRecords = g.Count(),
                LastOutTime = g.Max(x => x.l.CreatedTime)
            })
            .ToListAsync();

        // 分页在内存中处理：按 TotalValue 降序、ActivityId 升序排列
        var ordered = grouped
            .OrderByDescending(x => x.TotalValue)
            .ThenBy(x => x.ActivityId ?? 0)
            .ThenBy(x => x.ProductId)
            .ToList();
        var total = ordered.Count;
        var pageItems = ordered
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToList();

        var result = new PagedResponseDto<SampleActivityReportDto>
        {
            List = pageItems,
            Total = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
        return ApiResponseDto<PagedResponseDto<SampleActivityReportDto>>.Ok(result);
    }

    /// <summary>
    /// 获取样品/赠品按客户维度统计报表分页列表（P-SG-04）
    /// 数据源：SampleGiftReceive（样品领用，仅 CustomerId 有值的记录参与统计）
    /// 分页在内存中处理（GroupBy 后 EF Core 对 Skip/Take 翻译有限制）
    /// </summary>
    public async Task<ApiResponseDto<PagedResponseDto<SampleCustomerReportDto>>> GetReportByCustomerAsync(SampleCustomerReportQueryDto query)
    {
        if (!_currentUser.TenantId.HasValue || !_currentUser.StoreId.HasValue)
            return ApiResponseDto<PagedResponseDto<SampleCustomerReportDto>>.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId.Value;

        // 查询样品领用记录（CustomerId 必须有值），关联 Customer 取名称和手机号
        var queryable = from r in _dbContext.SampleGiftReceives
                        join c in _dbContext.Customers on r.CustomerId equals c.Id
                        where r.TenantId == tenantId && r.StoreId == storeId && r.CustomerId.HasValue && !c.IsDeleted
                        select new { r, c };

        if (query.CustomerId.HasValue)
            queryable = queryable.Where(x => x.r.CustomerId == query.CustomerId.Value);
        if (query.StartDate.HasValue)
            queryable = queryable.Where(x => x.r.ReceiveTime >= query.StartDate.Value);
        if (query.EndDate.HasValue)
            queryable = queryable.Where(x => x.r.ReceiveTime <= query.EndDate.Value);

        // 按客户分组聚合（含客户名称模糊匹配，需在 GroupBy 前过滤）
        // 客户名称模糊匹配无法在 GroupBy 后做，需先过滤再聚合
        if (!string.IsNullOrWhiteSpace(query.CustomerName))
            queryable = queryable.Where(x => x.c.Name.Contains(query.CustomerName));

        var grouped = await queryable
            .GroupBy(x => new { x.r.CustomerId, x.c.Name, x.c.Phone })
            .Select(g => new SampleCustomerReportDto
            {
                CustomerId = g.Key.CustomerId ?? 0,
                CustomerName = g.Key.Name,
                CustomerPhone = g.Key.Phone,
                ReceiveTimes = g.Count(),
                ReceiveCount = g.Sum(x => x.r.Quantity),
                LastReceiveTime = g.Max(x => x.r.ReceiveTime)
            })
            .ToListAsync();

        // 分页在内存中处理：按领用数量降序、CustomerId 升序排列
        var ordered = grouped
            .OrderByDescending(x => x.ReceiveCount)
            .ThenBy(x => x.CustomerId)
            .ToList();
        var total = ordered.Count;
        var pageItems = ordered
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToList();

        var result = new PagedResponseDto<SampleCustomerReportDto>
        {
            List = pageItems,
            Total = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
        return ApiResponseDto<PagedResponseDto<SampleCustomerReportDto>>.Ok(result);
    }

    // ========== 辅助方法 ==========

    /// <summary>
    /// 将 Product.Master 的字段填充到 ProductDto
    /// （Mapster 默认不会扁平映射 Master.Name -> Name，需手动填充）
    /// </summary>
    private static void FillMasterFields(ProductDto dto, ProductEntity product)
    {
        if (product.Master == null) return;
        dto.MasterId = product.MasterId;
        dto.Name = product.Master.Name;
        dto.Code = product.Master.Code;
        dto.CategoryId = product.Master.CategoryId;
        dto.CategoryName = product.Master.Category?.Name;
        dto.Type = product.Master.Type;
        dto.Spec = product.Master.Specification;
        dto.Unit = product.Master.Unit;
        dto.Brand = product.Master.Brand;
        dto.ImageUrl = product.Master.ImageUrl;
        dto.IsSalable = product.Master.IsSalable;
    }
}
