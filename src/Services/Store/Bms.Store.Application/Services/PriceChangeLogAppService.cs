using Mapster;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Bms.BuildingBlocks.Abstractions.Security;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.PriceChangeLogs;
using Bms.Store.Domain.Entities;
using PriceChangeLogEntity = Bms.Store.Domain.Entities.PriceChangeLog;
using Bms.Store.Infrastructure;

namespace Bms.Store.Application.Services;

/// <summary>
/// 价格变更记录应用服务实现
/// </summary>
public class PriceChangeLogAppService : IPriceChangeLogAppService
{
    /// <summary>
    /// 批量调价单次最多处理的商品数量
    /// 超过此值需分批调用，避免单次事务过大导致性能问题
    /// </summary>
    private const int BatchAdjustPriceMaxItems = 1000;

    private readonly StoreDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<PriceChangeLogCreateDto> _createValidator;
    private readonly IValidator<PriceChangeLogUpdateDto> _updateValidator;
    private readonly IValidator<BatchPriceAdjustDto> _batchAdjustValidator;

    public PriceChangeLogAppService(
        StoreDbContext dbContext,
        ICurrentUser currentUser,
        IValidator<PriceChangeLogCreateDto> createValidator,
        IValidator<PriceChangeLogUpdateDto> updateValidator,
        IValidator<BatchPriceAdjustDto> batchAdjustValidator)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _batchAdjustValidator = batchAdjustValidator;
    }

    /// <summary>
    /// 获取价格变更记录分页列表
    /// </summary>
    public async Task<ApiResponseDto<PagedResponseDto<PriceChangeLogDto>>> GetPagedListAsync(PriceChangeLogQueryDto query)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<PagedResponseDto<PriceChangeLogDto>>.Fail("无法确定当前租户", 401);

        var tenantId = _currentUser.TenantId.Value;
        var queryable = _dbContext.PriceChangeLogs
            .Where(p => p.TenantId == tenantId);

        if (query.ProductId.HasValue)
            queryable = queryable.Where(p => p.ProductId == query.ProductId.Value);

        var total = await queryable.CountAsync();
        var items = await queryable
            .OrderByDescending(p => p.CreatedTime)
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var result = new PagedResponseDto<PriceChangeLogDto>
        {
            List = items.Adapt<List<PriceChangeLogDto>>(),
            Total = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
        return ApiResponseDto<PagedResponseDto<PriceChangeLogDto>>.Ok(result);
    }

    /// <summary>
    /// 根据ID获取价格变更记录详情
    /// </summary>
    public async Task<ApiResponseDto<PriceChangeLogDto?>> GetByIdAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<PriceChangeLogDto?>.Fail("无法确定当前租户", 401);

        var entity = await _dbContext.PriceChangeLogs
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == _currentUser.TenantId.Value);
        if (entity == null)
            return ApiResponseDto<PriceChangeLogDto?>.Fail("价格变更记录不存在", 404);
        return ApiResponseDto<PriceChangeLogDto?>.Ok(entity.Adapt<PriceChangeLogDto>());
    }

    /// <summary>
    /// 创建价格变更记录
    /// </summary>
    public async Task<ApiResponseDto<PriceChangeLogDto>> CreateAsync(PriceChangeLogCreateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<PriceChangeLogDto>.Fail("无法确定当前租户", 401);

        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<PriceChangeLogDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var entity = dto.Adapt<PriceChangeLogEntity>();
        entity.TenantId = tenantId;
        entity.TenantCode = _currentUser.TenantCode ?? string.Empty;
        entity.CreatedTime = DateTime.Now;

        _dbContext.PriceChangeLogs.Add(entity);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto<PriceChangeLogDto>.Ok(entity.Adapt<PriceChangeLogDto>(), "创建成功");
    }

    /// <summary>
    /// 更新价格变更记录
    /// </summary>
    public async Task<ApiResponseDto<PriceChangeLogDto>> UpdateAsync(PriceChangeLogUpdateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<PriceChangeLogDto>.Fail("无法确定当前租户", 401);

        var validation = await _updateValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<PriceChangeLogDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var entity = await _dbContext.PriceChangeLogs
            .FirstOrDefaultAsync(p => p.Id == dto.Id && p.TenantId == tenantId);
        if (entity == null)
            return ApiResponseDto<PriceChangeLogDto>.Fail("价格变更记录不存在", 404);

        entity.ProductId = dto.ProductId;
        entity.OldPrice = dto.OldPrice;
        entity.NewPrice = dto.NewPrice;
        entity.ChangeTime = dto.ChangeTime;
        entity.OperatorId = dto.OperatorId;
        entity.Remark = dto.Remark;
        entity.UpdatedTime = DateTime.Now;

        await _dbContext.SaveChangesAsync();
        return ApiResponseDto<PriceChangeLogDto>.Ok(entity.Adapt<PriceChangeLogDto>(), "更新成功");
    }

    /// <summary>
    /// 删除价格变更记录（软删除）
    /// </summary>
    public async Task<ApiResponseDto> DeleteAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("无法确定当前租户", 401);

        var entity = await _dbContext.PriceChangeLogs
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == _currentUser.TenantId.Value);
        if (entity == null)
            return ApiResponseDto.Fail("价格变更记录不存在", 404);

        _dbContext.PriceChangeLogs.Remove(entity);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, "删除成功");
    }

    /// <summary>
    /// 批量删除价格变更记录（软删除）
    /// </summary>
    public async Task<ApiResponseDto> BatchDeleteAsync(List<long> ids)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("无法确定当前租户", 401);
        if (ids == null || !ids.Any())
            return ApiResponseDto.Fail("请选择要删除的数据", 400);

        var entities = await _dbContext.PriceChangeLogs
            .Where(p => ids.Contains(p.Id) && p.TenantId == _currentUser.TenantId.Value)
            .ToListAsync();

        _dbContext.PriceChangeLogs.RemoveRange(entities);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, $"成功删除 {entities.Count} 条数据");
    }

    /// <summary>
    /// 批量调价：按范围（商品ID列表/分类/供应商/全部）批量更新商品价格并自动记录价格变更日志
    /// 同事务内完成价格更新与日志记录，保证数据一致性
    /// 单次最多处理 1000 个商品，超过需分批调用
    /// </summary>
    public async Task<ApiResponseDto<BatchPriceAdjustResultDto>> BatchAdjustPriceAsync(BatchPriceAdjustDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<BatchPriceAdjustResultDto>.Fail("无法确定当前租户", 401);

        var validation = await _batchAdjustValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<BatchPriceAdjustResultDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;

        // 按 RangeType 构造商品查询
        IQueryable<Product> query = _dbContext.Products
            .Where(p => !p.IsDeleted && p.TenantId == tenantId);

        switch (dto.RangeType)
        {
            case 1:
                query = query.Where(p => dto.ProductIds!.Contains(p.Id));
                break;
            case 2:
                query = query.Where(p => p.CategoryId == dto.ProductCategoryId!.Value);
                break;
            case 3:
                query = query.Where(p => p.SupplierId == dto.SupplierId!.Value);
                break;
            case 4:
                // 全部商品，不加额外条件
                break;
        }

        // 单次最多 1000 个商品
        var productCount = await query.CountAsync();
        if (productCount == 0)
            return ApiResponseDto<BatchPriceAdjustResultDto>.Fail("未查询到符合条件的商品", 404);
        if (productCount > BatchAdjustPriceMaxItems)
            return ApiResponseDto<BatchPriceAdjustResultDto>.Fail(
                $"符合条件的商品共 {productCount} 个，超过单次最大处理量 {BatchAdjustPriceMaxItems}，请缩小范围或分批处理", 400);

        var products = await query.ToListAsync();
        var now = DateTime.Now;
        var operatorId = _currentUser.UserId;
        var tenantCode = _currentUser.TenantCode ?? string.Empty;
        var remark = string.IsNullOrWhiteSpace(dto.Remark) ? "批量调价" : dto.Remark;

        var result = new BatchPriceAdjustResultDto();

        foreach (var product in products)
        {
            var oldPrice = product.Price;
            var newPrice = dto.AdjustType switch
            {
                1 => oldPrice * (1 + dto.AdjustValue / 100m),
                2 => oldPrice + dto.AdjustValue,
                3 => dto.AdjustValue,
                _ => oldPrice
            };

            // 保留两位小数
            newPrice = Math.Round(newPrice, 2);

            // 校验：新价格必须 >= 0
            if (newPrice < 0)
            {
                result.FailedCount++;
                result.Failures.Add(new BatchPriceAdjustFailureItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Reason = $"计算后的新价格 {newPrice} 为负数"
                });
                continue;
            }

            // 价格未变化则跳过
            if (oldPrice == newPrice)
            {
                result.SkippedCount++;
                continue;
            }

            // 更新商品价格
            product.Price = newPrice;
            product.UpdatedTime = now;

            // 创建价格变更记录
            _dbContext.PriceChangeLogs.Add(new PriceChangeLogEntity
            {
                ProductId = product.Id,
                OldPrice = oldPrice,
                NewPrice = newPrice,
                ChangeTime = now,
                OperatorId = operatorId,
                Remark = remark,
                TenantId = tenantId,
                TenantCode = tenantCode,
                CreatedTime = now
            });
            result.SuccessCount++;
        }

        await _dbContext.SaveChangesAsync();
        return ApiResponseDto<BatchPriceAdjustResultDto>.Ok(
            result,
            $"成功调价 {result.SuccessCount} 个，跳过 {result.SkippedCount} 个，失败 {result.FailedCount} 个");
    }
}
