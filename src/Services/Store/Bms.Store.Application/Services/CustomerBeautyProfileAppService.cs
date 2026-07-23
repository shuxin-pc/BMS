using Mapster;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Bms.BuildingBlocks.Abstractions.Security;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Customers;
using CustomerBeautyProfileEntity = Bms.Store.Domain.Entities.CustomerBeautyProfile;
using Bms.Store.Infrastructure;

namespace Bms.Store.Application.Services;

/// <summary>
/// 客户美容档案应用服务实现
/// </summary>
public class CustomerBeautyProfileAppService : ICustomerBeautyProfileAppService
{
    private readonly StoreDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<CustomerBeautyProfileCreateDto> _createValidator;
    private readonly IValidator<CustomerBeautyProfileUpdateDto> _updateValidator;

    public CustomerBeautyProfileAppService(
        StoreDbContext dbContext,
        ICurrentUser currentUser,
        IValidator<CustomerBeautyProfileCreateDto> createValidator,
        IValidator<CustomerBeautyProfileUpdateDto> updateValidator)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>
    /// 获取客户美容档案分页列表
    /// </summary>
    public async Task<ApiResponseDto<PagedResponseDto<CustomerBeautyProfileDto>>> GetPagedListAsync(CustomerBeautyProfileQueryDto query)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<PagedResponseDto<CustomerBeautyProfileDto>>.Fail("无法确定当前租户", 401);

        var tenantId = _currentUser.TenantId.Value;
        var queryable = _dbContext.CustomerBeautyProfiles
            .Where(p => !p.IsDeleted && p.TenantId == tenantId);

        if (query.CustomerId.HasValue)
            queryable = queryable.Where(p => p.CustomerId == query.CustomerId.Value);

        var total = await queryable.CountAsync();
        var items = await queryable
            .OrderByDescending(p => p.CreatedTime)
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var result = new PagedResponseDto<CustomerBeautyProfileDto>
        {
            List = items.Adapt<List<CustomerBeautyProfileDto>>(),
            Total = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
        return ApiResponseDto<PagedResponseDto<CustomerBeautyProfileDto>>.Ok(result);
    }

    /// <summary>
    /// 根据ID获取客户美容档案详情
    /// </summary>
    public async Task<ApiResponseDto<CustomerBeautyProfileDto?>> GetByIdAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<CustomerBeautyProfileDto?>.Fail("无法确定当前租户", 401);

        var entity = await _dbContext.CustomerBeautyProfiles
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted && p.TenantId == _currentUser.TenantId.Value);
        if (entity == null)
            return ApiResponseDto<CustomerBeautyProfileDto?>.Fail("客户美容档案不存在", 404);
        return ApiResponseDto<CustomerBeautyProfileDto?>.Ok(entity.Adapt<CustomerBeautyProfileDto>());
    }

    /// <summary>
    /// 创建客户美容档案
    /// </summary>
    public async Task<ApiResponseDto<CustomerBeautyProfileDto>> CreateAsync(CustomerBeautyProfileCreateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<CustomerBeautyProfileDto>.Fail("无法确定当前租户", 401);

        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<CustomerBeautyProfileDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var entity = dto.Adapt<CustomerBeautyProfileEntity>();
        entity.TenantId = tenantId;
        entity.TenantCode = _currentUser.TenantCode ?? string.Empty;
        entity.CreatedTime = DateTime.Now;

        _dbContext.CustomerBeautyProfiles.Add(entity);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto<CustomerBeautyProfileDto>.Ok(entity.Adapt<CustomerBeautyProfileDto>(), "创建成功");
    }

    /// <summary>
    /// 更新客户美容档案
    /// </summary>
    public async Task<ApiResponseDto<CustomerBeautyProfileDto>> UpdateAsync(CustomerBeautyProfileUpdateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<CustomerBeautyProfileDto>.Fail("无法确定当前租户", 401);

        var validation = await _updateValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<CustomerBeautyProfileDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var entity = await _dbContext.CustomerBeautyProfiles
            .FirstOrDefaultAsync(p => p.Id == dto.Id && !p.IsDeleted && p.TenantId == tenantId);
        if (entity == null)
            return ApiResponseDto<CustomerBeautyProfileDto>.Fail("客户美容档案不存在", 404);

        entity.CustomerId = dto.CustomerId;
        entity.SkinType = dto.SkinType;
        entity.Sensitivity = dto.Sensitivity;
        entity.HairType = dto.HairType;
        entity.AllergyHistory = dto.AllergyHistory;
        entity.Remark = dto.Remark;
        entity.UpdatedTime = DateTime.Now;

        await _dbContext.SaveChangesAsync();
        return ApiResponseDto<CustomerBeautyProfileDto>.Ok(entity.Adapt<CustomerBeautyProfileDto>(), "更新成功");
    }

    /// <summary>
    /// 删除客户美容档案（软删除）
    /// </summary>
    public async Task<ApiResponseDto> DeleteAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("无法确定当前租户", 401);

        var entity = await _dbContext.CustomerBeautyProfiles
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted && p.TenantId == _currentUser.TenantId.Value);
        if (entity == null)
            return ApiResponseDto.Fail("客户美容档案不存在", 404);

        entity.IsDeleted = true;
        entity.UpdatedTime = DateTime.Now;
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, "删除成功");
    }

    /// <summary>
    /// 批量删除客户美容档案（软删除）
    /// </summary>
    public async Task<ApiResponseDto> BatchDeleteAsync(List<long> ids)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("无法确定当前租户", 401);
        if (ids == null || !ids.Any())
            return ApiResponseDto.Fail("请选择要删除的数据", 400);

        var entities = await _dbContext.CustomerBeautyProfiles
            .Where(p => ids.Contains(p.Id) && !p.IsDeleted && p.TenantId == _currentUser.TenantId.Value)
            .ToListAsync();

        foreach (var entity in entities)
        {
            entity.IsDeleted = true;
            entity.UpdatedTime = DateTime.Now;
        }
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, $"成功删除 {entities.Count} 条数据");
    }
}
