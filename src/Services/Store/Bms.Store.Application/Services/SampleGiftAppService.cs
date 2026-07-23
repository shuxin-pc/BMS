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
/// 提供档案管理 CRUD、库存查询、统计报表功能
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
            return ApiResponseDto<PagedResponseDto<ProductDto>>.Fail("无法确定当前租户", 401);

        var tenantId = _currentUser.TenantId.Value;
        var queryable = _dbContext.Products
            .Include(p => p.Category)
            .Where(p => !p.IsDeleted && p.TenantId == tenantId && (p.Type == 4 || p.Type == 5));

        if (!string.IsNullOrWhiteSpace(query.Name))
            queryable = queryable.Where(p => p.Name.Contains(query.Name));
        if (!string.IsNullOrWhiteSpace(query.Code))
            queryable = queryable.Where(p => p.Code.Contains(query.Code));
        if (query.Type.HasValue)
            queryable = queryable.Where(p => p.Type == query.Type.Value);
        if (query.Status.HasValue)
            queryable = queryable.Where(p => p.Status == query.Status.Value);

        var total = await queryable.CountAsync();
        var items = await queryable
            .OrderByDescending(p => p.CreatedTime)
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var dtoList = items.Adapt<List<ProductDto>>();
        var result = new PagedResponseDto<ProductDto>
        {
            List = dtoList,
            Total = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
        return ApiResponseDto<PagedResponseDto<ProductDto>>.Ok(result);
    }

    /// <summary>
    /// 根据ID获取样品/赠品详情
    /// </summary>
    public async Task<ApiResponseDto<ProductDto?>> GetByIdAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<ProductDto?>.Fail("无法确定当前租户", 401);

        var tenantId = _currentUser.TenantId.Value;
        var product = await _dbContext.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted && p.TenantId == tenantId
                && (p.Type == 4 || p.Type == 5));
        if (product == null)
            return ApiResponseDto<ProductDto?>.Fail("样品/赠品不存在", 404);

        return ApiResponseDto<ProductDto?>.Ok(product.Adapt<ProductDto>());
    }

    /// <summary>
    /// 创建样品/赠品（强制 Type=4或5）
    /// </summary>
    public async Task<ApiResponseDto<ProductDto>> CreateAsync(ProductCreateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<ProductDto>.Fail("无法确定当前租户", 401);

        // 验证类型必须是样品(4)或赠品(5)
        if (dto.Type != 4 && dto.Type != 5)
            return ApiResponseDto<ProductDto>.Fail("样品/赠品类型必须为4（样品）或5（赠品）", 400);

        var tenantId = _currentUser.TenantId.Value;
        var codeExists = await _dbContext.Products
            .AnyAsync(p => p.Code == dto.Code && p.TenantId == tenantId && !p.IsDeleted);
        if (codeExists)
            return ApiResponseDto<ProductDto>.Fail($"商品编码 {dto.Code} 已存在", 400);

        var product = dto.Adapt<ProductEntity>();
        product.TenantId = tenantId;
        product.TenantCode = _currentUser.TenantCode ?? string.Empty;
        product.CreatedTime = DateTime.Now;
        // 样品/赠品强制不可销售（B6.1 "不可销售"标识，与 SampleGiftAppService 业务约束一致）
        product.IsSalable = false;

        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        // 重新加载导航属性以获取 CategoryName
        await _dbContext.Entry(product).Reference(p => p.Category).LoadAsync();

        return ApiResponseDto<ProductDto>.Ok(product.Adapt<ProductDto>(), "创建成功");
    }

    /// <summary>
    /// 更新样品/赠品
    /// </summary>
    public async Task<ApiResponseDto<ProductDto>> UpdateAsync(ProductUpdateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<ProductDto>.Fail("无法确定当前租户", 401);

        // 验证类型必须是样品(4)或赠品(5)
        if (dto.Type != 4 && dto.Type != 5)
            return ApiResponseDto<ProductDto>.Fail("样品/赠品类型必须为4（样品）或5（赠品）", 400);

        var tenantId = _currentUser.TenantId.Value;
        var product = await _dbContext.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == dto.Id && !p.IsDeleted && p.TenantId == tenantId
                && (p.Type == 4 || p.Type == 5));
        if (product == null)
            return ApiResponseDto<ProductDto>.Fail("样品/赠品不存在", 404);

        // 编码变更时检查唯一性
        if (product.Code != dto.Code)
        {
            var codeExists = await _dbContext.Products
                .AnyAsync(p => p.Code == dto.Code && p.TenantId == tenantId && !p.IsDeleted && p.Id != dto.Id);
            if (codeExists)
                return ApiResponseDto<ProductDto>.Fail($"商品编码 {dto.Code} 已存在", 400);
        }

        // 手动更新主表字段（避免覆盖审计字段）
        product.Name = dto.Name;
        product.Code = dto.Code;
        product.CategoryId = dto.CategoryId;
        product.Type = dto.Type;
        product.Specification = dto.Spec;
        product.Unit = dto.Unit;
        product.Price = dto.Price;
        product.CostPrice = dto.CostPrice;
        product.LowStockThreshold = dto.LowStockThreshold;
        product.ExpiryAlertDays = dto.ExpiryAlertDays;
        product.OverstockThreshold = dto.OverstockThreshold;
        product.ImageUrl = dto.ImageUrl;
        product.Status = dto.Status;
        product.Remark = dto.Description;
        // 样品/赠品强制不可销售（与 CreateAsync 一致，B6.1 "不可销售"标识）
        product.IsSalable = false;
        product.UpdatedTime = DateTime.Now;

        await _dbContext.SaveChangesAsync();
        return ApiResponseDto<ProductDto>.Ok(product.Adapt<ProductDto>(), "更新成功");
    }

    /// <summary>
    /// 删除样品/赠品（软删除）
    /// </summary>
    public async Task<ApiResponseDto> DeleteAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("无法确定当前租户", 401);

        var tenantId = _currentUser.TenantId.Value;
        var product = await _dbContext.Products
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted && p.TenantId == tenantId
                && (p.Type == 4 || p.Type == 5));
        if (product == null)
            return ApiResponseDto.Fail("样品/赠品不存在", 404);

        product.IsDeleted = true;
        product.UpdatedTime = DateTime.Now;
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, "删除成功");
    }

    /// <summary>
    /// 批量删除样品/赠品（软删除）
    /// </summary>
    public async Task<ApiResponseDto> BatchDeleteAsync(List<long> ids)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("无法确定当前租户", 401);
        if (ids == null || !ids.Any())
            return ApiResponseDto.Fail("请选择要删除的数据", 400);

        var tenantId = _currentUser.TenantId.Value;
        var products = await _dbContext.Products
            .Where(p => ids.Contains(p.Id) && !p.IsDeleted && p.TenantId == tenantId
                && (p.Type == 4 || p.Type == 5))
            .ToListAsync();

        foreach (var product in products)
        {
            product.IsDeleted = true;
            product.UpdatedTime = DateTime.Now;
        }

        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, $"成功删除 {products.Count} 条数据");
    }

    // ========== 库存查询 ==========

    /// <summary>
    /// 获取样品/赠品库存查询分页列表
    /// 关联 Inventory 汇总表，计算库存状态
    /// </summary>
    public async Task<ApiResponseDto<PagedResponseDto<SampleInventoryDto>>> GetInventoryListAsync(SampleInventoryQueryDto query)
    {
        if (!_currentUser.TenantId.HasValue || !_currentUser.StoreId.HasValue)
            return ApiResponseDto<PagedResponseDto<SampleInventoryDto>>.Fail("无法确定当前租户或门店", 401);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId.Value;

        // 查询样品/赠品 Product，左连接 Inventory 汇总表
        var queryable = from p in _dbContext.Products
                        where !p.IsDeleted && p.TenantId == tenantId && (p.Type == 4 || p.Type == 5)
                        join i in _dbContext.Inventories
                            on new { ProductId = p.Id, TenantId = tenantId, StoreId = storeId }
                            equals new { ProductId = i.ProductId, TenantId = i.TenantId, StoreId = i.StoreId }
                            into inventories
                        from i in inventories.DefaultIfEmpty()
                        select new { p, i };

        if (!string.IsNullOrWhiteSpace(query.Name))
            queryable = queryable.Where(x => x.p.Name.Contains(query.Name));
        if (query.Type.HasValue)
            queryable = queryable.Where(x => x.p.Type == query.Type.Value);

        var total = await queryable.CountAsync();
        var rawItems = await queryable
            .OrderByDescending(x => x.p.CreatedTime)
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        // 计算库存状态并映射到 DTO
        var dtoList = rawItems.Select(x => new SampleInventoryDto
        {
            Id = x.p.Id,
            Name = x.p.Name,
            Code = x.p.Code,
            Type = x.p.Type,
            Unit = x.p.Unit,
            CurrentStock = x.i?.Quantity ?? 0,
            AlertQuantity = x.i?.AlertQuantity ?? x.p.LowStockThreshold,
            InventoryStatus = CalculateInventoryStatus(x.i?.Quantity ?? 0, x.i?.AlertQuantity ?? x.p.LowStockThreshold),
            LastInboundTime = null // 上次入库时间需要查 InventoryLog，暂不提供
        }).ToList();

        // 库存状态筛选（需要在内存中过滤）
        if (query.InventoryStatus.HasValue)
            dtoList = dtoList.Where(d => d.InventoryStatus == query.InventoryStatus.Value).ToList();

        var result = new PagedResponseDto<SampleInventoryDto>
        {
            List = dtoList,
            Total = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
        return ApiResponseDto<PagedResponseDto<SampleInventoryDto>>.Ok(result);
    }

    // ========== 统计报表 ==========

    /// <summary>
    /// 获取样品/赠品统计报表分页列表
    /// 聚合 SampleGiftReceive（领用）和 SampleGiftOut（出库）数量
    /// </summary>
    public async Task<ApiResponseDto<PagedResponseDto<SampleReportDto>>> GetReportListAsync(SampleReportQueryDto query)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<PagedResponseDto<SampleReportDto>>.Fail("无法确定当前租户", 401);

        var tenantId = _currentUser.TenantId.Value;
        var queryable = _dbContext.Products
            .Where(p => !p.IsDeleted && p.TenantId == tenantId && (p.Type == 4 || p.Type == 5));

        if (!string.IsNullOrWhiteSpace(query.Name))
            queryable = queryable.Where(p => p.Name.Contains(query.Name));
        if (query.Type.HasValue)
            queryable = queryable.Where(p => p.Type == query.Type.Value);

        var total = await queryable.CountAsync();
        var products = await queryable
            .OrderByDescending(p => p.CreatedTime)
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var productIds = products.Select(p => p.Id).ToList();

        // 批量查询领用数量
        var receiveQuery = _dbContext.SampleGiftReceives
            .Where(r => r.TenantId == tenantId && productIds.Contains(r.ProductId));
        if (query.StartDate.HasValue)
            receiveQuery = receiveQuery.Where(r => r.ReceiveTime >= query.StartDate.Value);
        if (query.EndDate.HasValue)
            receiveQuery = receiveQuery.Where(r => r.ReceiveTime <= query.EndDate.Value);
        var receiveCounts = await receiveQuery
            .GroupBy(r => r.ProductId)
            .Select(g => new { ProductId = g.Key, Total = g.Sum(r => r.Quantity) })
            .ToDictionaryAsync(x => x.ProductId, x => x.Total);

        // 批量查询出库数量
        var outQuery = _dbContext.SampleGiftOuts
            .Where(o => o.TenantId == tenantId && productIds.Contains(o.ProductId));
        if (query.StartDate.HasValue)
            outQuery = outQuery.Where(o => o.OutTime >= query.StartDate.Value);
        if (query.EndDate.HasValue)
            outQuery = outQuery.Where(o => o.OutTime <= query.EndDate.Value);
        var outCounts = await outQuery
            .GroupBy(o => o.ProductId)
            .Select(g => new { ProductId = g.Key, Total = g.Sum(o => o.Quantity) })
            .ToDictionaryAsync(x => x.ProductId, x => x.Total);

        // 批量查询当前库存
        var stockDict = await _dbContext.Inventories
            .Where(i => i.TenantId == tenantId && productIds.Contains(i.ProductId))
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
                Name = p.Name,
                Type = p.Type,
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
    /// 数据源：SampleGiftOut（赠品出库），按 ActivityId + ProductId 聚合
    /// 分页在内存中处理（GroupBy 后 EF Core 对 Skip/Take 翻译有限制）
    /// </summary>
    public async Task<ApiResponseDto<PagedResponseDto<SampleActivityReportDto>>> GetReportByActivityAsync(SampleActivityReportQueryDto query)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<PagedResponseDto<SampleActivityReportDto>>.Fail("无法确定当前租户", 401);

        var tenantId = _currentUser.TenantId.Value;

        // 查询赠品出库记录，关联 Product 取名称和成本价
        var queryable = from o in _dbContext.SampleGiftOuts
                        join p in _dbContext.Products on o.ProductId equals p.Id
                        where o.TenantId == tenantId && !p.IsDeleted
                        select new { o, p };

        if (query.ActivityId.HasValue)
            queryable = queryable.Where(x => x.o.ActivityId == query.ActivityId.Value);
        if (query.ProductId.HasValue)
            queryable = queryable.Where(x => x.o.ProductId == query.ProductId.Value);
        if (query.StartDate.HasValue)
            queryable = queryable.Where(x => x.o.OutTime >= query.StartDate.Value);
        if (query.EndDate.HasValue)
            queryable = queryable.Where(x => x.o.OutTime <= query.EndDate.Value);

        // 按 ActivityId + ProductId 分组聚合（EF Core 可翻译 GroupBy + Sum）
        // CostPrice 为 decimal? 类型，求和前需处理 null（按 0 计入）
        var grouped = await queryable
            .GroupBy(x => new { x.o.ActivityId, x.o.ProductId })
            .Select(g => new SampleActivityReportDto
            {
                ActivityId = g.Key.ActivityId,
                ProductId = g.Key.ProductId,
                ProductName = g.Select(x => x.p.Name).FirstOrDefault() ?? string.Empty,
                OutboundCount = g.Sum(x => x.o.Quantity),
                TotalValue = g.Sum(x => x.o.Quantity * (x.p.CostPrice ?? 0m)),
                OutboundRecords = g.Count(),
                LastOutTime = g.Max(x => x.o.OutTime)
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
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<PagedResponseDto<SampleCustomerReportDto>>.Fail("无法确定当前租户", 401);

        var tenantId = _currentUser.TenantId.Value;

        // 查询样品领用记录（CustomerId 必须有值），关联 Customer 取名称和手机号
        var queryable = from r in _dbContext.SampleGiftReceives
                        join c in _dbContext.Customers on r.CustomerId equals c.Id
                        where r.TenantId == tenantId && r.CustomerId.HasValue && !c.IsDeleted
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
    /// 计算库存状态：1=充足，2=偏低，3=不足
    /// 阈值未配置（null）时不产生"偏低"预警，与 InventoryAlertAppService 低库存预警行为一致
    /// </summary>
    private static int CalculateInventoryStatus(decimal currentStock, decimal? alertQuantity)
    {
        if (currentStock <= 0)
            return 3; // 不足
        if (alertQuantity.HasValue && currentStock <= alertQuantity.Value)
            return 2; // 偏低
        return 1; // 充足
    }
}
