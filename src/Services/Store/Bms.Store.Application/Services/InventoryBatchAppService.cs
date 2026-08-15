using Mapster;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Bms.BuildingBlocks.Abstractions.Security;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.InventoryBatches;
using InventoryBatchEntity = Bms.Store.Domain.Entities.InventoryBatch;
using Bms.Store.Infrastructure;

namespace Bms.Store.Application.Services;

/// <summary>
/// 库存批次应用服务实现
/// </summary>
public class InventoryBatchAppService : IInventoryBatchAppService
{
    private readonly StoreDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<InventoryBatchCreateDto> _createValidator;
    private readonly IValidator<InventoryBatchUpdateDto> _updateValidator;

    public InventoryBatchAppService(
        StoreDbContext dbContext,
        ICurrentUser currentUser,
        IValidator<InventoryBatchCreateDto> createValidator,
        IValidator<InventoryBatchUpdateDto> updateValidator)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>
    /// 获取库存批次分页列表
    /// </summary>
    public async Task<ApiResponseDto<PagedResponseDto<InventoryBatchDto>>> GetPagedListAsync(InventoryBatchQueryDto query)
    {
        if (!_currentUser.TenantId.HasValue || !_currentUser.StoreId.HasValue)
            return ApiResponseDto<PagedResponseDto<InventoryBatchDto>>.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId.Value;
        var queryable = _dbContext.InventoryBatches
            .Where(b => b.TenantId == tenantId && b.StoreId == storeId);

        if (query.ProductId.HasValue)
            queryable = queryable.Where(b => b.ProductId == query.ProductId.Value);
        if (!string.IsNullOrWhiteSpace(query.BatchNo))
            queryable = queryable.Where(b => b.BatchNo.Contains(query.BatchNo));
        if (query.Status.HasValue)
            queryable = queryable.Where(b => b.Status == query.Status.Value);

        var total = await queryable.CountAsync();
        var items = await queryable
            .OrderByDescending(b => b.CreatedTime)
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var result = new PagedResponseDto<InventoryBatchDto>
        {
            List = items.Adapt<List<InventoryBatchDto>>(),
            Total = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
        return ApiResponseDto<PagedResponseDto<InventoryBatchDto>>.Ok(result);
    }

    /// <summary>
    /// 根据ID获取库存批次详情
    /// </summary>
    public async Task<ApiResponseDto<InventoryBatchDto?>> GetByIdAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue || !_currentUser.StoreId.HasValue)
            return ApiResponseDto<InventoryBatchDto?>.Fail("登录状态异常，请重新登录", 401);

        var entity = await _dbContext.InventoryBatches
            .FirstOrDefaultAsync(b => b.Id == id && b.TenantId == _currentUser.TenantId.Value && b.StoreId == _currentUser.StoreId.Value);
        if (entity == null)
            return ApiResponseDto<InventoryBatchDto?>.Fail("库存批次不存在", 404);
        return ApiResponseDto<InventoryBatchDto?>.Ok(entity.Adapt<InventoryBatchDto>());
    }

    /// <summary>
    /// 创建库存批次
    /// </summary>
    public async Task<ApiResponseDto<InventoryBatchDto>> CreateAsync(InventoryBatchCreateDto dto)
    {
        if (!_currentUser.TenantId.HasValue || !_currentUser.StoreId.HasValue)
            return ApiResponseDto<InventoryBatchDto>.Fail("登录状态异常，请重新登录", 401);

        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<InventoryBatchDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId.Value;
        var entity = dto.Adapt<InventoryBatchEntity>();

        // 过期日期：若录入"生产日期+保质期天数"则自动计算覆盖传入值，与 UpdateAsync 保持一致
        if (entity.ProductionDate.HasValue && entity.ShelfLifeDays.HasValue)
        {
            entity.ExpirationDate = entity.ProductionDate.Value.AddDays(entity.ShelfLifeDays.Value);
        }

        entity.TenantId = tenantId;
        entity.TenantCode = _currentUser.TenantCode ?? string.Empty;
        entity.StoreId = storeId;
        entity.CreatedTime = DateTime.Now;

        _dbContext.InventoryBatches.Add(entity);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto<InventoryBatchDto>.Ok(entity.Adapt<InventoryBatchDto>(), "创建成功");
    }

    /// <summary>
    /// 更新库存批次
    /// </summary>
    public async Task<ApiResponseDto<InventoryBatchDto>> UpdateAsync(InventoryBatchUpdateDto dto)
    {
        if (!_currentUser.TenantId.HasValue || !_currentUser.StoreId.HasValue)
            return ApiResponseDto<InventoryBatchDto>.Fail("登录状态异常，请重新登录", 401);

        var validation = await _updateValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<InventoryBatchDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId.Value;
        var entity = await _dbContext.InventoryBatches
            .FirstOrDefaultAsync(b => b.Id == dto.Id && b.TenantId == tenantId && b.StoreId == storeId);
        if (entity == null)
            return ApiResponseDto<InventoryBatchDto>.Fail("库存批次不存在", 404);

        entity.ProductId = dto.ProductId;
        entity.BatchNo = dto.BatchNo;
        entity.Quantity = dto.Quantity;
        entity.UnitPrice = dto.UnitPrice;
        entity.ProductionDate = dto.ProductionDate;
        entity.ShelfLifeDays = dto.ShelfLifeDays;

        // 过期日期计算：若录入"生产日期+保质期天数"，自动计算过期日期
        if (entity.ProductionDate.HasValue && entity.ShelfLifeDays.HasValue)
        {
            entity.ExpirationDate = entity.ProductionDate.Value.AddDays(entity.ShelfLifeDays.Value);
        }
        else
        {
            entity.ExpirationDate = dto.ExpirationDate;
        }

        entity.PurchaseDate = dto.PurchaseDate;
        entity.Status = dto.Status;
        entity.Remark = dto.Remark;
        entity.UpdatedTime = DateTime.Now;

        await _dbContext.SaveChangesAsync();
        return ApiResponseDto<InventoryBatchDto>.Ok(entity.Adapt<InventoryBatchDto>(), "更新成功");
    }

    /// <summary>
    /// 删除库存批次（物理删除）
    /// </summary>
    public async Task<ApiResponseDto> DeleteAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue || !_currentUser.StoreId.HasValue)
            return ApiResponseDto.Fail("登录状态异常，请重新登录", 401);

        var entity = await _dbContext.InventoryBatches
            .FirstOrDefaultAsync(b => b.Id == id && b.TenantId == _currentUser.TenantId.Value && b.StoreId == _currentUser.StoreId.Value);
        if (entity == null)
            return ApiResponseDto.Fail("库存批次不存在", 404);

        _dbContext.InventoryBatches.Remove(entity);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, "删除成功");
    }

    /// <summary>
    /// 批量删除库存批次（物理删除）
    /// </summary>
    public async Task<ApiResponseDto> BatchDeleteAsync(List<long> ids)
    {
        if (!_currentUser.TenantId.HasValue || !_currentUser.StoreId.HasValue)
            return ApiResponseDto.Fail("登录状态异常，请重新登录", 401);
        if (ids == null || !ids.Any())
            return ApiResponseDto.Fail("请选择要删除的数据", 400);

        var entities = await _dbContext.InventoryBatches
            .Where(b => ids.Contains(b.Id) && b.TenantId == _currentUser.TenantId.Value && b.StoreId == _currentUser.StoreId.Value)
            .ToListAsync();

        _dbContext.InventoryBatches.RemoveRange(entities);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, $"成功删除 {entities.Count} 条数据");
    }

    /// <summary>
    /// 获取效期信息分页列表（仅包含已设置过期日期的批次）
    /// </summary>
    public async Task<ApiResponseDto<PagedResponseDto<ExpiryDto>>> GetExpiryListAsync(ExpiryQueryDto query)
    {
        if (!_currentUser.TenantId.HasValue || !_currentUser.StoreId.HasValue)
            return ApiResponseDto<PagedResponseDto<ExpiryDto>>.Fail("登录状态异常，请重新登录", 401);

        var allItems = (await QueryExpiryDataAsync(query.ProductName, query.Status))
            .Where(d => d.ExpirationDate.HasValue)
            .ToList();

        var total = allItems.Count;
        var pagedItems = allItems
            .OrderBy(d => d.ExpirationDate)     // 近效期优先
            .ThenBy(d => d.CreatedTime)          // 兜底稳定排序
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToList();

        var result = new PagedResponseDto<ExpiryDto>
        {
            List = pagedItems,
            Total = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
        return ApiResponseDto<PagedResponseDto<ExpiryDto>>.Ok(result);
    }

    /// <summary>
    /// 查询效期数据（join Product 取名称编码，批量填充门店名称，内存计算剩余天数和状态）。
    /// 包含未填到期日期的批次，其 RemainingDays 为 null、Status 为 normal（不参与效期预警）。
    /// </summary>
    private async Task<List<ExpiryDto>> QueryExpiryDataAsync(string? productName, string? statusFilter)
    {
        var tenantId = _currentUser.TenantId!.Value;
        var storeId = _currentUser.StoreId!.Value;
        var now = DateTime.Now;

        var queryable = from b in _dbContext.InventoryBatches
                        join p in _dbContext.Products on b.ProductId equals p.Id
                        where b.TenantId == tenantId && b.StoreId == storeId
                        select new { b, p };

        if (!string.IsNullOrWhiteSpace(productName))
            queryable = queryable.Where(x => x.p.Master.Name.Contains(productName));

        var rawData = await queryable
            .Select(x => new
            {
                x.b.Id,
                ProductName = x.p.Master.Name,
                ProductCode = x.p.Master.Code,
                x.b.BatchNo,
                x.b.ExpirationDate,
                x.b.PurchaseDate,
                x.b.StoreId,
                x.b.CreatedTime,
                ExpiryAlertDays = x.p.ExpiryAlertDays
            })
            .ToListAsync();

        // 批量查询门店名称避免 N+1
        var storeIds = rawData.Select(x => x.StoreId).Distinct().ToList();
        var storeNames = await _dbContext.Stores
            .Where(s => storeIds.Contains(s.Id))
            .Select(s => new { s.Id, s.Name })
            .ToDictionaryAsync(s => s.Id, s => s.Name);

        var dtos = rawData.Select(x =>
        {
            var storeName = storeNames.TryGetValue(x.StoreId, out var name) ? name : null;

            // 无效期批次：无剩余天数概念，状态为 normal（不参与效期预警）
            if (!x.ExpirationDate.HasValue)
            {
                return new ExpiryDto
                {
                    Id = x.Id,
                    ProductName = x.ProductName,
                    ProductCode = x.ProductCode,
                    BatchNo = x.BatchNo,
                    ExpirationDate = null,
                    PurchaseDate = x.PurchaseDate,
                    RemainingDays = null,
                    Status = "normal",
                    StoreName = storeName,
                    CreatedTime = x.CreatedTime
                };
            }

            var remainingDays = (x.ExpirationDate.Value - now).Days;
            // expired 为客观过期事实，不依赖阈值配置；expiring 为预警状态，依赖商品级 ExpiryAlertDays
            // 阈值未配置（null）时不产生 expiring 预警，与 InventoryAlertAppService.ScanExpiryAsync 行为一致
            string status;
            if (remainingDays < 0)
            {
                status = "expired";
            }
            else if (x.ExpiryAlertDays.HasValue && remainingDays <= x.ExpiryAlertDays.Value)
            {
                status = "expiring";
            }
            else
            {
                status = "normal";
            }
            return new ExpiryDto
            {
                Id = x.Id,
                ProductName = x.ProductName,
                ProductCode = x.ProductCode,
                BatchNo = x.BatchNo,
                ExpirationDate = x.ExpirationDate,
                PurchaseDate = x.PurchaseDate,
                RemainingDays = remainingDays,
                Status = status,
                StoreName = storeName,
                CreatedTime = x.CreatedTime
            };
        });

        if (!string.IsNullOrWhiteSpace(statusFilter))
            dtos = dtos.Where(d => d.Status == statusFilter);

        return dtos.ToList();
    }

    /// <summary>
    /// 按商品ID查询可用效期选项列表（用于 POS 效期选择）。
    /// 查询条件：Status=1(在库) && Quantity>0，包含未填到期日期的批次。
    /// 有日期批次按 ExpirationDate 升序在前；无效期批次聚合为一组排末尾按 CreatedTime 升序；第一项标记为推荐。
    /// </summary>
    public async Task<ApiResponseDto<List<ProductExpiryOptionDto>>> GetExpiryOptionsByProductIdAsync(long productId)
    {
        if (!_currentUser.TenantId.HasValue || !_currentUser.StoreId.HasValue)
            return ApiResponseDto<List<ProductExpiryOptionDto>>.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId.Value;
        var now = DateTime.Now;

        // 查询该商品所有在库且有余量的批次（包含未填到期日期的批次）
        var batches = await _dbContext.InventoryBatches
            .Where(b => b.ProductId == productId
                && b.TenantId == tenantId
                && b.StoreId == storeId
                && b.Status == 1
                && b.Quantity > 0)
            .ToListAsync();

        if (!batches.Any())
            return ApiResponseDto<List<ProductExpiryOptionDto>>.Ok(new List<ProductExpiryOptionDto>());

        // 有日期批次按 ExpirationDate.Date 分组聚合
        var withDateOptions = batches
            .Where(b => b.ExpirationDate.HasValue)
            .GroupBy(b => b.ExpirationDate!.Value.Date)
            .Select(g => new ProductExpiryOptionDto
            {
                ExpirationDate = g.Key,
                TotalQuantity = g.Sum(b => b.Quantity),
                EarliestPurchaseDate = g.Min(b => b.PurchaseDate),
                RemainingDays = (g.Key - now.Date).Days,
                IsRecommended = false,
                IsNoExpiry = false,
                CreatedTime = g.Min(b => b.CreatedTime)
            })
            .OrderBy(o => o.ExpirationDate)
            .ToList();

        // 无效期批次聚合为一组，排末尾按 CreatedTime 升序
        var noExpiryOptions = batches
            .Where(b => !b.ExpirationDate.HasValue)
            .GroupBy(b => 0)
            .Select(g => new ProductExpiryOptionDto
            {
                ExpirationDate = null,
                TotalQuantity = g.Sum(b => b.Quantity),
                EarliestPurchaseDate = g.Min(b => b.PurchaseDate),
                RemainingDays = null,
                IsRecommended = false,
                IsNoExpiry = true,
                CreatedTime = g.Min(b => b.CreatedTime)
            })
            .OrderBy(o => o.CreatedTime)
            .ToList();

        // 有日期批次在前升序，无效期批次排末尾
        var options = withDateOptions.Concat(noExpiryOptions).ToList();

        // 第一项（近效期）标记为推荐
        if (options.Any())
        {
            options[0].IsRecommended = true;
        }

        return ApiResponseDto<List<ProductExpiryOptionDto>>.Ok(options);
    }
}
