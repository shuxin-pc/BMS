using Mapster;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Bms.BuildingBlocks.Abstractions.Security;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Orders;
using OrderItemEntity = Bms.Store.Domain.Entities.OrderItem;
using Bms.Store.Infrastructure;

namespace Bms.Store.Application.Services;

/// <summary>
/// 订单明细应用服务实现
/// </summary>
public class OrderItemAppService : IOrderItemAppService
{
    private readonly StoreDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<OrderItemCreateDto> _createValidator;
    private readonly IValidator<OrderItemUpdateDto> _updateValidator;

    public OrderItemAppService(
        StoreDbContext dbContext,
        ICurrentUser currentUser,
        IValidator<OrderItemCreateDto> createValidator,
        IValidator<OrderItemUpdateDto> updateValidator)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>
    /// 获取订单明细分页列表
    /// </summary>
    public async Task<ApiResponseDto<PagedResponseDto<OrderItemDto>>> GetPagedListAsync(OrderItemQueryDto query)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<PagedResponseDto<OrderItemDto>>.Fail("无法确定当前租户", 401);

        var tenantId = _currentUser.TenantId.Value;
        var queryable = _dbContext.OrderItems
            .Where(i => i.TenantId == tenantId);

        if (query.OrderId.HasValue)
            queryable = queryable.Where(i => i.OrderId == query.OrderId.Value);

        var total = await queryable.CountAsync();
        var items = await queryable
            .OrderByDescending(i => i.CreatedTime)
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var result = new PagedResponseDto<OrderItemDto>
        {
            List = items.Adapt<List<OrderItemDto>>(),
            Total = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
        return ApiResponseDto<PagedResponseDto<OrderItemDto>>.Ok(result);
    }

    /// <summary>
    /// 根据ID获取订单明细详情
    /// </summary>
    public async Task<ApiResponseDto<OrderItemDto?>> GetByIdAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<OrderItemDto?>.Fail("无法确定当前租户", 401);

        var entity = await _dbContext.OrderItems
            .Include(i => i.Batches)
            .FirstOrDefaultAsync(i => i.Id == id && i.TenantId == _currentUser.TenantId.Value);
        if (entity == null)
            return ApiResponseDto<OrderItemDto?>.Fail("订单明细不存在", 404);
        return ApiResponseDto<OrderItemDto?>.Ok(entity.Adapt<OrderItemDto>());
    }

    /// <summary>
    /// 创建订单明细
    /// </summary>
    public async Task<ApiResponseDto<OrderItemDto>> CreateAsync(OrderItemCreateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<OrderItemDto>.Fail("无法确定当前租户", 401);

        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<OrderItemDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var entity = dto.Adapt<OrderItemEntity>();
        entity.TenantId = tenantId;
        entity.TenantCode = _currentUser.TenantCode ?? string.Empty;
        entity.CreatedTime = DateTime.Now;

        _dbContext.OrderItems.Add(entity);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto<OrderItemDto>.Ok(entity.Adapt<OrderItemDto>(), "创建成功");
    }

    /// <summary>
    /// 更新订单明细
    /// </summary>
    public async Task<ApiResponseDto<OrderItemDto>> UpdateAsync(OrderItemUpdateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<OrderItemDto>.Fail("无法确定当前租户", 401);

        var validation = await _updateValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<OrderItemDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var entity = await _dbContext.OrderItems
            .FirstOrDefaultAsync(i => i.Id == dto.Id && i.TenantId == tenantId);
        if (entity == null)
            return ApiResponseDto<OrderItemDto>.Fail("订单明细不存在", 404);

        entity.OrderId = dto.OrderId;
        entity.ProductId = dto.ProductId;
        entity.ProductName = dto.ProductName;
        entity.ProductCode = dto.ProductCode;
        entity.TechnicianId = dto.TechnicianId;
        entity.TechnicianSource = dto.TechnicianSource;
        entity.Quantity = dto.Quantity;
        entity.Price = dto.Price;
        entity.DiscountRate = dto.DiscountRate;
        entity.DiscountedAmount = dto.DiscountedAmount;
        entity.TechnicianFee = dto.TechnicianFee;
        entity.Remark = dto.Remark;
        entity.UpdatedTime = DateTime.Now;

        await _dbContext.SaveChangesAsync();
        return ApiResponseDto<OrderItemDto>.Ok(entity.Adapt<OrderItemDto>(), "更新成功");
    }

    /// <summary>
    /// 删除订单明细
    /// </summary>
    public async Task<ApiResponseDto> DeleteAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("无法确定当前租户", 401);

        var entity = await _dbContext.OrderItems
            .FirstOrDefaultAsync(i => i.Id == id && i.TenantId == _currentUser.TenantId.Value);
        if (entity == null)
            return ApiResponseDto.Fail("订单明细不存在", 404);

        _dbContext.OrderItems.Remove(entity);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, "删除成功");
    }

    /// <summary>
    /// 批量删除订单明细
    /// </summary>
    public async Task<ApiResponseDto> BatchDeleteAsync(List<long> ids)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("无法确定当前租户", 401);
        if (ids == null || !ids.Any())
            return ApiResponseDto.Fail("请选择要删除的数据", 400);

        var entities = await _dbContext.OrderItems
            .Where(i => ids.Contains(i.Id) && i.TenantId == _currentUser.TenantId.Value)
            .ToListAsync();

        _dbContext.OrderItems.RemoveRange(entities);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, $"成功删除 {entities.Count} 条数据");
    }
}
