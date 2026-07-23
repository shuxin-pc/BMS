using Mapster;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Bms.BuildingBlocks.Abstractions.Security;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.PurchaseOrders;
using PurchaseOrderItemEntity = Bms.Store.Domain.Entities.PurchaseOrderItem;
using Bms.Store.Infrastructure;

namespace Bms.Store.Application.Services;

/// <summary>
/// 采购订单明细应用服务实现
/// </summary>
public class PurchaseOrderItemAppService : IPurchaseOrderItemAppService
{
    private readonly StoreDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<PurchaseOrderItemCreateDto> _createValidator;
    private readonly IValidator<PurchaseOrderItemUpdateDto> _updateValidator;

    public PurchaseOrderItemAppService(
        StoreDbContext dbContext,
        ICurrentUser currentUser,
        IValidator<PurchaseOrderItemCreateDto> createValidator,
        IValidator<PurchaseOrderItemUpdateDto> updateValidator)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>
    /// 获取采购订单明细分页列表
    /// </summary>
    public async Task<ApiResponseDto<PagedResponseDto<PurchaseOrderItemDto>>> GetPagedListAsync(PurchaseOrderItemQueryDto query)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<PagedResponseDto<PurchaseOrderItemDto>>.Fail("无法确定当前租户", 401);

        var tenantId = _currentUser.TenantId.Value;
        var queryable = _dbContext.PurchaseOrderItems
            .Where(p => p.TenantId == tenantId);

        if (query.PurchaseOrderId.HasValue)
            queryable = queryable.Where(p => p.PurchaseOrderId == query.PurchaseOrderId.Value);
        if (query.ProductId.HasValue)
            queryable = queryable.Where(p => p.ProductId == query.ProductId.Value);

        var total = await queryable.CountAsync();
        var items = await queryable
            .OrderByDescending(p => p.CreatedTime)
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var result = new PagedResponseDto<PurchaseOrderItemDto>
        {
            List = items.Adapt<List<PurchaseOrderItemDto>>(),
            Total = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
        return ApiResponseDto<PagedResponseDto<PurchaseOrderItemDto>>.Ok(result);
    }

    /// <summary>
    /// 根据ID获取采购订单明细详情
    /// </summary>
    public async Task<ApiResponseDto<PurchaseOrderItemDto?>> GetByIdAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<PurchaseOrderItemDto?>.Fail("无法确定当前租户", 401);

        var entity = await _dbContext.PurchaseOrderItems
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == _currentUser.TenantId.Value);
        if (entity == null)
            return ApiResponseDto<PurchaseOrderItemDto?>.Fail("采购订单明细不存在", 404);
        return ApiResponseDto<PurchaseOrderItemDto?>.Ok(entity.Adapt<PurchaseOrderItemDto>());
    }

    /// <summary>
    /// 创建采购订单明细
    /// </summary>
    public async Task<ApiResponseDto<PurchaseOrderItemDto>> CreateAsync(PurchaseOrderItemCreateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<PurchaseOrderItemDto>.Fail("无法确定当前租户", 401);

        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<PurchaseOrderItemDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var entity = dto.Adapt<PurchaseOrderItemEntity>();
        entity.TenantId = tenantId;
        entity.TenantCode = _currentUser.TenantCode ?? string.Empty;
        entity.CreatedTime = DateTime.Now;

        _dbContext.PurchaseOrderItems.Add(entity);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto<PurchaseOrderItemDto>.Ok(entity.Adapt<PurchaseOrderItemDto>(), "创建成功");
    }

    /// <summary>
    /// 更新采购订单明细
    /// </summary>
    public async Task<ApiResponseDto<PurchaseOrderItemDto>> UpdateAsync(PurchaseOrderItemUpdateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<PurchaseOrderItemDto>.Fail("无法确定当前租户", 401);

        var validation = await _updateValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<PurchaseOrderItemDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var entity = await _dbContext.PurchaseOrderItems
            .FirstOrDefaultAsync(p => p.Id == dto.Id && p.TenantId == tenantId);
        if (entity == null)
            return ApiResponseDto<PurchaseOrderItemDto>.Fail("采购订单明细不存在", 404);

        entity.PurchaseOrderId = dto.PurchaseOrderId;
        entity.ProductId = dto.ProductId;
        entity.Quantity = dto.Quantity;
        entity.UnitPrice = dto.UnitPrice;
        entity.TotalPrice = dto.TotalPrice;
        entity.BatchNo = dto.BatchNo;
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

        entity.Remark = dto.Remark;
        entity.UpdatedTime = DateTime.Now;

        await _dbContext.SaveChangesAsync();
        return ApiResponseDto<PurchaseOrderItemDto>.Ok(entity.Adapt<PurchaseOrderItemDto>(), "更新成功");
    }

    /// <summary>
    /// 删除采购订单明细
    /// </summary>
    public async Task<ApiResponseDto> DeleteAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("无法确定当前租户", 401);

        var entity = await _dbContext.PurchaseOrderItems
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == _currentUser.TenantId.Value);
        if (entity == null)
            return ApiResponseDto.Fail("采购订单明细不存在", 404);

        _dbContext.PurchaseOrderItems.Remove(entity);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, "删除成功");
    }

    /// <summary>
    /// 批量删除采购订单明细
    /// </summary>
    public async Task<ApiResponseDto> BatchDeleteAsync(List<long> ids)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("无法确定当前租户", 401);
        if (ids == null || !ids.Any())
            return ApiResponseDto.Fail("请选择要删除的数据", 400);

        var entities = await _dbContext.PurchaseOrderItems
            .Where(p => ids.Contains(p.Id) && p.TenantId == _currentUser.TenantId.Value)
            .ToListAsync();

        _dbContext.PurchaseOrderItems.RemoveRange(entities);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, $"成功删除 {entities.Count} 条数据");
    }
}
