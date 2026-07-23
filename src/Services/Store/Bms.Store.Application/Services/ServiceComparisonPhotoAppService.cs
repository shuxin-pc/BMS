using Mapster;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Bms.BuildingBlocks.Abstractions.Security;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Customers;
using ServiceComparisonPhotoEntity = Bms.Store.Domain.Entities.ServiceComparisonPhoto;
using Bms.Store.Infrastructure;

namespace Bms.Store.Application.Services;

public class ServiceComparisonPhotoAppService : IServiceComparisonPhotoAppService
{
    private readonly StoreDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<ServiceComparisonPhotoCreateDto> _createValidator;
    private readonly IValidator<ServiceComparisonPhotoUpdateDto> _updateValidator;

    public ServiceComparisonPhotoAppService(
        StoreDbContext dbContext,
        ICurrentUser currentUser,
        IValidator<ServiceComparisonPhotoCreateDto> createValidator,
        IValidator<ServiceComparisonPhotoUpdateDto> updateValidator)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<ApiResponseDto<PagedResponseDto<ServiceComparisonPhotoDto>>> GetPagedListAsync(ServiceComparisonPhotoQueryDto query)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<PagedResponseDto<ServiceComparisonPhotoDto>>.Fail("无法确定当前租户", 401);

        var tenantId = _currentUser.TenantId.Value;
        var queryable = _dbContext.ServiceComparisonPhotos
            .Where(p => p.TenantId == tenantId);

        if (query.CustomerId.HasValue)
            queryable = queryable.Where(p => p.CustomerId == query.CustomerId.Value);

        var total = await queryable.CountAsync();
        var items = await queryable
            .OrderByDescending(p => p.CreatedTime)
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var result = new PagedResponseDto<ServiceComparisonPhotoDto>
        {
            List = items.Adapt<List<ServiceComparisonPhotoDto>>(),
            Total = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
        return ApiResponseDto<PagedResponseDto<ServiceComparisonPhotoDto>>.Ok(result);
    }

    public async Task<ApiResponseDto<ServiceComparisonPhotoDto?>> GetByIdAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<ServiceComparisonPhotoDto?>.Fail("无法确定当前租户", 401);

        var entity = await _dbContext.ServiceComparisonPhotos
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == _currentUser.TenantId.Value);
        if (entity == null)
            return ApiResponseDto<ServiceComparisonPhotoDto?>.Fail("服务对比照片不存在", 404);
        return ApiResponseDto<ServiceComparisonPhotoDto?>.Ok(entity.Adapt<ServiceComparisonPhotoDto>());
    }

    public async Task<ApiResponseDto<ServiceComparisonPhotoDto>> CreateAsync(ServiceComparisonPhotoCreateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<ServiceComparisonPhotoDto>.Fail("无法确定当前租户", 401);

        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<ServiceComparisonPhotoDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;

        // 校验关联订单归属当前租户
        var orderExists = await _dbContext.Orders
            .AnyAsync(o => o.Id == dto.OrderId && o.TenantId == tenantId);
        if (!orderExists)
            return ApiResponseDto<ServiceComparisonPhotoDto>.Fail("关联订单不存在", 400);

        var entity = dto.Adapt<ServiceComparisonPhotoEntity>();
        entity.TenantId = tenantId;
        entity.TenantCode = _currentUser.TenantCode ?? string.Empty;
        entity.CreatedTime = DateTime.Now;

        _dbContext.ServiceComparisonPhotos.Add(entity);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto<ServiceComparisonPhotoDto>.Ok(entity.Adapt<ServiceComparisonPhotoDto>(), "创建成功");
    }

    public async Task<ApiResponseDto<ServiceComparisonPhotoDto>> UpdateAsync(ServiceComparisonPhotoUpdateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<ServiceComparisonPhotoDto>.Fail("无法确定当前租户", 401);

        var validation = await _updateValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<ServiceComparisonPhotoDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var entity = await _dbContext.ServiceComparisonPhotos
            .FirstOrDefaultAsync(p => p.Id == dto.Id && p.TenantId == tenantId);
        if (entity == null)
            return ApiResponseDto<ServiceComparisonPhotoDto>.Fail("服务对比照片不存在", 404);

        // 校验关联订单归属当前租户
        var orderExists = await _dbContext.Orders
            .AnyAsync(o => o.Id == dto.OrderId && o.TenantId == tenantId);
        if (!orderExists)
            return ApiResponseDto<ServiceComparisonPhotoDto>.Fail("关联订单不存在", 400);

        entity.CustomerId = dto.CustomerId;
        entity.OrderId = dto.OrderId;
        entity.ServiceItem = dto.ServiceItem;
        entity.PhotoDate = dto.PhotoDate;
        entity.PhotoType = dto.PhotoType;
        entity.PhotoUrl = dto.PhotoUrl;
        entity.Remark = dto.Remark;
        entity.UpdatedTime = DateTime.Now;

        await _dbContext.SaveChangesAsync();
        return ApiResponseDto<ServiceComparisonPhotoDto>.Ok(entity.Adapt<ServiceComparisonPhotoDto>(), "更新成功");
    }

    public async Task<ApiResponseDto> DeleteAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("无法确定当前租户", 401);

        var entity = await _dbContext.ServiceComparisonPhotos
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == _currentUser.TenantId.Value);
        if (entity == null)
            return ApiResponseDto.Fail("服务对比照片不存在", 404);

        _dbContext.ServiceComparisonPhotos.Remove(entity);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, "删除成功");
    }

    public async Task<ApiResponseDto> BatchDeleteAsync(List<long> ids)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("无法确定当前租户", 401);
        if (ids == null || !ids.Any())
            return ApiResponseDto.Fail("请选择要删除的数据", 400);

        var entities = await _dbContext.ServiceComparisonPhotos
            .Where(p => ids.Contains(p.Id) && p.TenantId == _currentUser.TenantId.Value)
            .ToListAsync();

        _dbContext.ServiceComparisonPhotos.RemoveRange(entities);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, $"成功删除 {entities.Count} 条数据");
    }
}
