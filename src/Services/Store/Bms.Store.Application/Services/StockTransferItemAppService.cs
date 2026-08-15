using Mapster;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Bms.BuildingBlocks.Abstractions.Security;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.StockTransfers;
using StockTransferItemEntity = Bms.Store.Domain.Entities.StockTransferItem;
using Bms.Store.Infrastructure;

namespace Bms.Store.Application.Services;

/// <summary>
/// 库存调拨单明细应用服务实现
/// </summary>
public class StockTransferItemAppService : IStockTransferItemAppService
{
    private readonly StoreDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<StockTransferItemCreateDto> _createValidator;
    private readonly IValidator<StockTransferItemUpdateDto> _updateValidator;

    public StockTransferItemAppService(
        StoreDbContext dbContext,
        ICurrentUser currentUser,
        IValidator<StockTransferItemCreateDto> createValidator,
        IValidator<StockTransferItemUpdateDto> updateValidator)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>
    /// 获取库存调拨单明细分页列表
    /// </summary>
    public async Task<ApiResponseDto<PagedResponseDto<StockTransferItemDto>>> GetPagedListAsync(StockTransferItemQueryDto query)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<PagedResponseDto<StockTransferItemDto>>.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;
        var queryable = _dbContext.StockTransferItems
            .Where(i => i.TenantId == tenantId);

        if (query.StockTransferId.HasValue)
            queryable = queryable.Where(i => i.StockTransferId == query.StockTransferId.Value);
        if (query.ProductId.HasValue)
            queryable = queryable.Where(i => i.ProductId == query.ProductId.Value);
        if (!string.IsNullOrWhiteSpace(query.BatchNo))
            queryable = queryable.Where(i => i.BatchNo != null && i.BatchNo.Contains(query.BatchNo));

        var total = await queryable.CountAsync();
        var items = await queryable
            .OrderByDescending(i => i.CreatedTime)
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var result = new PagedResponseDto<StockTransferItemDto>
        {
            List = items.Adapt<List<StockTransferItemDto>>(),
            Total = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
        return ApiResponseDto<PagedResponseDto<StockTransferItemDto>>.Ok(result);
    }

    /// <summary>
    /// 根据ID获取库存调拨单明细详情
    /// </summary>
    public async Task<ApiResponseDto<StockTransferItemDto?>> GetByIdAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<StockTransferItemDto?>.Fail("登录状态异常，请重新登录", 401);

        var entity = await _dbContext.StockTransferItems
            .FirstOrDefaultAsync(i => i.Id == id && i.TenantId == _currentUser.TenantId.Value);
        if (entity == null)
            return ApiResponseDto<StockTransferItemDto?>.Fail("库存调拨单明细不存在", 404);
        return ApiResponseDto<StockTransferItemDto?>.Ok(entity.Adapt<StockTransferItemDto>());
    }

    /// <summary>
    /// 创建库存调拨单明细
    /// </summary>
    public async Task<ApiResponseDto<StockTransferItemDto>> CreateAsync(StockTransferItemCreateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<StockTransferItemDto>.Fail("登录状态异常，请重新登录", 401);

        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<StockTransferItemDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var entity = dto.Adapt<StockTransferItemEntity>();
        entity.TenantId = tenantId;
        entity.TenantCode = _currentUser.TenantCode ?? string.Empty;
        entity.CreatedTime = DateTime.Now;

        _dbContext.StockTransferItems.Add(entity);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto<StockTransferItemDto>.Ok(entity.Adapt<StockTransferItemDto>(), "创建成功");
    }

    /// <summary>
    /// 更新库存调拨单明细
    /// </summary>
    public async Task<ApiResponseDto<StockTransferItemDto>> UpdateAsync(StockTransferItemUpdateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<StockTransferItemDto>.Fail("登录状态异常，请重新登录", 401);

        var validation = await _updateValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<StockTransferItemDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var entity = await _dbContext.StockTransferItems
            .FirstOrDefaultAsync(i => i.Id == dto.Id && i.TenantId == tenantId);
        if (entity == null)
            return ApiResponseDto<StockTransferItemDto>.Fail("库存调拨单明细不存在", 404);

        entity.StockTransferId = dto.StockTransferId;
        entity.ProductId = dto.ProductId;
        entity.Quantity = dto.Quantity;
        entity.BatchNo = dto.BatchNo;
        entity.Remark = dto.Remark;
        entity.UpdatedTime = DateTime.Now;

        await _dbContext.SaveChangesAsync();
        return ApiResponseDto<StockTransferItemDto>.Ok(entity.Adapt<StockTransferItemDto>(), "更新成功");
    }

    /// <summary>
    /// 删除库存调拨单明细
    /// </summary>
    public async Task<ApiResponseDto> DeleteAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("登录状态异常，请重新登录", 401);

        var entity = await _dbContext.StockTransferItems
            .FirstOrDefaultAsync(i => i.Id == id && i.TenantId == _currentUser.TenantId.Value);
        if (entity == null)
            return ApiResponseDto.Fail("库存调拨单明细不存在", 404);

        _dbContext.StockTransferItems.Remove(entity);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, "删除成功");
    }

    /// <summary>
    /// 批量删除库存调拨单明细
    /// </summary>
    public async Task<ApiResponseDto> BatchDeleteAsync(List<long> ids)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("登录状态异常，请重新登录", 401);
        if (ids == null || !ids.Any())
            return ApiResponseDto.Fail("请选择要删除的数据", 400);

        var entities = await _dbContext.StockTransferItems
            .Where(i => ids.Contains(i.Id) && i.TenantId == _currentUser.TenantId.Value)
            .ToListAsync();

        _dbContext.StockTransferItems.RemoveRange(entities);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, $"成功删除 {entities.Count} 条数据");
    }
}
