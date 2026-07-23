using Mapster;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Bms.BuildingBlocks.Abstractions.Security;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Customers;
using CustomerPreferenceEntity = Bms.Store.Domain.Entities.CustomerPreference;
using Bms.Store.Infrastructure;

namespace Bms.Store.Application.Services;

public class CustomerPreferenceAppService : ICustomerPreferenceAppService
{
    private readonly StoreDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<CustomerPreferenceCreateDto> _createValidator;
    private readonly IValidator<CustomerPreferenceUpdateDto> _updateValidator;

    public CustomerPreferenceAppService(
        StoreDbContext dbContext,
        ICurrentUser currentUser,
        IValidator<CustomerPreferenceCreateDto> createValidator,
        IValidator<CustomerPreferenceUpdateDto> updateValidator)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<ApiResponseDto<PagedResponseDto<CustomerPreferenceDto>>> GetPagedListAsync(CustomerPreferenceQueryDto query)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<PagedResponseDto<CustomerPreferenceDto>>.Fail("无法确定当前租户", 401);

        var tenantId = _currentUser.TenantId.Value;
        var queryable = _dbContext.CustomerPreferences
            .Where(p => !p.IsDeleted && p.TenantId == tenantId);

        if (query.CustomerId.HasValue)
            queryable = queryable.Where(p => p.CustomerId == query.CustomerId.Value);

        var total = await queryable.CountAsync();
        var items = await queryable
            .OrderByDescending(p => p.CreatedTime)
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var result = new PagedResponseDto<CustomerPreferenceDto>
        {
            List = items.Adapt<List<CustomerPreferenceDto>>(),
            Total = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
        return ApiResponseDto<PagedResponseDto<CustomerPreferenceDto>>.Ok(result);
    }

    public async Task<ApiResponseDto<CustomerPreferenceDto?>> GetByIdAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<CustomerPreferenceDto?>.Fail("无法确定当前租户", 401);

        var entity = await _dbContext.CustomerPreferences
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted && p.TenantId == _currentUser.TenantId.Value);
        if (entity == null)
            return ApiResponseDto<CustomerPreferenceDto?>.Fail("客户偏好不存在", 404);
        return ApiResponseDto<CustomerPreferenceDto?>.Ok(entity.Adapt<CustomerPreferenceDto>());
    }

    public async Task<ApiResponseDto<CustomerPreferenceDto>> CreateAsync(CustomerPreferenceCreateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<CustomerPreferenceDto>.Fail("无法确定当前租户", 401);

        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<CustomerPreferenceDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var entity = dto.Adapt<CustomerPreferenceEntity>();
        entity.TenantId = tenantId;
        entity.TenantCode = _currentUser.TenantCode ?? string.Empty;
        entity.CreatedTime = DateTime.Now;

        _dbContext.CustomerPreferences.Add(entity);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto<CustomerPreferenceDto>.Ok(entity.Adapt<CustomerPreferenceDto>(), "创建成功");
    }

    public async Task<ApiResponseDto<CustomerPreferenceDto>> UpdateAsync(CustomerPreferenceUpdateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<CustomerPreferenceDto>.Fail("无法确定当前租户", 401);

        var validation = await _updateValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<CustomerPreferenceDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var entity = await _dbContext.CustomerPreferences
            .FirstOrDefaultAsync(p => p.Id == dto.Id && !p.IsDeleted && p.TenantId == tenantId);
        if (entity == null)
            return ApiResponseDto<CustomerPreferenceDto>.Fail("客户偏好不存在", 404);

        entity.CustomerId = dto.CustomerId;
        entity.TechniquePressure = dto.TechniquePressure;
        entity.Temperature = dto.Temperature;
        entity.MusicPreference = dto.MusicPreference;
        entity.PreferredTechnicianId = dto.PreferredTechnicianId;
        entity.Remark = dto.Remark;
        entity.UpdatedTime = DateTime.Now;

        await _dbContext.SaveChangesAsync();
        return ApiResponseDto<CustomerPreferenceDto>.Ok(entity.Adapt<CustomerPreferenceDto>(), "更新成功");
    }

    public async Task<ApiResponseDto> DeleteAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("无法确定当前租户", 401);

        var entity = await _dbContext.CustomerPreferences
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted && p.TenantId == _currentUser.TenantId.Value);
        if (entity == null)
            return ApiResponseDto.Fail("客户偏好不存在", 404);

        entity.IsDeleted = true;
        entity.UpdatedTime = DateTime.Now;
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, "删除成功");
    }

    public async Task<ApiResponseDto> BatchDeleteAsync(List<long> ids)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("无法确定当前租户", 401);
        if (ids == null || !ids.Any())
            return ApiResponseDto.Fail("请选择要删除的数据", 400);

        var entities = await _dbContext.CustomerPreferences
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
