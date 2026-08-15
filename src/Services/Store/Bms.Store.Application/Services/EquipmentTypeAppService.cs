using Mapster;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Bms.BuildingBlocks.Abstractions.Security;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.EquipmentTypes;
using Bms.Store.Domain.Entities;
using Bms.Store.Infrastructure;

namespace Bms.Store.Application.Services;

/// <summary>
/// 设备类型应用服务实现
/// 设备类型为租户级共享数据（所有门店共用同一套，不按门店隔离）
/// </summary>
public class EquipmentTypeAppService : IEquipmentTypeAppService
{
    private readonly StoreDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<EquipmentTypeCreateDto> _createValidator;
    private readonly IValidator<EquipmentTypeUpdateDto> _updateValidator;

    public EquipmentTypeAppService(
        StoreDbContext dbContext,
        ICurrentUser currentUser,
        IValidator<EquipmentTypeCreateDto> createValidator,
        IValidator<EquipmentTypeUpdateDto> updateValidator)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<ApiResponseDto<PagedResponseDto<EquipmentTypeDto>>> GetPagedListAsync(EquipmentTypeQueryDto query)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<PagedResponseDto<EquipmentTypeDto>>.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;
        var queryable = _dbContext.EquipmentTypes
            .Where(t => !t.IsDeleted && t.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(query.Name))
            queryable = queryable.Where(t => t.Name.Contains(query.Name));
        if (!string.IsNullOrWhiteSpace(query.Code))
            queryable = queryable.Where(t => t.Code.Contains(query.Code));
        if (query.IsActive.HasValue)
            queryable = queryable.Where(t => t.IsActive == query.IsActive.Value);

        var total = await queryable.CountAsync();
        var items = await queryable
            .OrderByDescending(t => t.CreatedTime)
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var result = new PagedResponseDto<EquipmentTypeDto>
        {
            List = await FillEquipmentCountsAsync(items, tenantId),
            Total = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
        return ApiResponseDto<PagedResponseDto<EquipmentTypeDto>>.Ok(result);
    }

    public async Task<ApiResponseDto<List<EquipmentTypeDto>>> GetAllAsync()
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<List<EquipmentTypeDto>>.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;
        var items = await _dbContext.EquipmentTypes
            .Where(t => !t.IsDeleted && t.TenantId == tenantId && t.IsActive)
            .OrderBy(t => t.Name)
            .ToListAsync();

        return ApiResponseDto<List<EquipmentTypeDto>>.Ok(await FillEquipmentCountsAsync(items, tenantId));
    }

    public async Task<ApiResponseDto<EquipmentTypeDto?>> GetByIdAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<EquipmentTypeDto?>.Fail("登录状态异常，请重新登录", 401);

        var type = await _dbContext.EquipmentTypes
            .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted && t.TenantId == _currentUser.TenantId.Value);
        if (type == null)
            return ApiResponseDto<EquipmentTypeDto?>.Fail("设备类型不存在", 404);

        var dto = type.Adapt<EquipmentTypeDto>();
        dto.EquipmentCount = await _dbContext.Equipments
            .CountAsync(e => e.EquipmentTypeId == id && e.TenantId == _currentUser.TenantId.Value && !e.IsDeleted);
        return ApiResponseDto<EquipmentTypeDto?>.Ok(dto);
    }

    public async Task<ApiResponseDto<EquipmentTypeDto>> CreateAsync(EquipmentTypeCreateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<EquipmentTypeDto>.Fail("登录状态异常，请重新登录", 401);

        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<EquipmentTypeDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var codeExists = await _dbContext.EquipmentTypes
            .AnyAsync(t => t.Code == dto.Code && t.TenantId == tenantId && !t.IsDeleted);
        if (codeExists)
            return ApiResponseDto<EquipmentTypeDto>.Fail($"类型编码 {dto.Code} 已存在", 400);

        var type = dto.Adapt<EquipmentType>();
        type.TenantId = tenantId;
        type.TenantCode = _currentUser.TenantCode ?? string.Empty;
        // 设备类型为租户级共享数据，不按门店隔离，StoreId 置 0
        type.StoreId = 0;
        type.StoreCode = string.Empty;
        type.CreatedTime = DateTime.Now;

        _dbContext.EquipmentTypes.Add(type);
        await _dbContext.SaveChangesAsync();

        var resultDto = type.Adapt<EquipmentTypeDto>();
        resultDto.EquipmentCount = 0;
        return ApiResponseDto<EquipmentTypeDto>.Ok(resultDto, "创建成功");
    }

    public async Task<ApiResponseDto<EquipmentTypeDto>> UpdateAsync(EquipmentTypeUpdateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<EquipmentTypeDto>.Fail("登录状态异常，请重新登录", 401);

        var validation = await _updateValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<EquipmentTypeDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var type = await _dbContext.EquipmentTypes
            .FirstOrDefaultAsync(t => t.Id == dto.Id && !t.IsDeleted && t.TenantId == tenantId);
        if (type == null)
            return ApiResponseDto<EquipmentTypeDto>.Fail("设备类型不存在", 404);

        if (type.Code != dto.Code)
        {
            var codeExists = await _dbContext.EquipmentTypes
                .AnyAsync(t => t.Code == dto.Code && t.TenantId == tenantId && !t.IsDeleted && t.Id != dto.Id);
            if (codeExists)
                return ApiResponseDto<EquipmentTypeDto>.Fail($"类型编码 {dto.Code} 已存在", 400);
        }

        type.Name = dto.Name;
        type.Code = dto.Code;
        type.Category = dto.Category;
        type.Spec = dto.Spec;
        type.Description = dto.Description;
        type.IsActive = dto.IsActive;
        type.UpdatedTime = DateTime.Now;

        await _dbContext.SaveChangesAsync();

        var resultDto = type.Adapt<EquipmentTypeDto>();
        resultDto.EquipmentCount = await _dbContext.Equipments
            .CountAsync(e => e.EquipmentTypeId == dto.Id && e.TenantId == tenantId && !e.IsDeleted);
        return ApiResponseDto<EquipmentTypeDto>.Ok(resultDto, "更新成功");
    }

    public async Task<ApiResponseDto> DeleteAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;
        var type = await _dbContext.EquipmentTypes
            .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted && t.TenantId == tenantId);
        if (type == null)
            return ApiResponseDto.Fail("设备类型不存在", 404);

        // 删除保护：被设备实例引用时禁止删除
        var equipmentCount = await _dbContext.Equipments
            .CountAsync(e => e.EquipmentTypeId == id && e.TenantId == tenantId && !e.IsDeleted);
        if (equipmentCount > 0)
            return ApiResponseDto.Fail($"该类型已被 {equipmentCount} 台设备引用，请先在设备档案中调整后再删除", 400);

        // 删除保护：被服务项目关联时禁止删除（关联表不启用软删除）
        var serviceCount = await _dbContext.ServiceProductEquipments
            .CountAsync(r => r.EquipmentTypeId == id && r.TenantId == tenantId);
        if (serviceCount > 0)
            return ApiResponseDto.Fail($"该类型已被 {serviceCount} 个服务项目关联，请先在服务项目中移除后再删除", 400);

        type.IsDeleted = true;
        type.UpdatedTime = DateTime.Now;
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, "删除成功");
    }

    /// <summary>
    /// 批量填充设备类型的关联设备数量
    /// </summary>
    private async Task<List<EquipmentTypeDto>> FillEquipmentCountsAsync(List<EquipmentType> items, long tenantId)
    {
        var typeIds = items.Select(t => t.Id).ToList();
        var dtos = items.Adapt<List<EquipmentTypeDto>>();

        if (typeIds.Count == 0)
            return dtos;

        var counts = await _dbContext.Equipments
            .Where(e => !e.IsDeleted && e.TenantId == tenantId && typeIds.Contains(e.EquipmentTypeId))
            .GroupBy(e => e.EquipmentTypeId)
            .Select(g => new { EquipmentTypeId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.EquipmentTypeId, x => x.Count);

        foreach (var dto in dtos)
            dto.EquipmentCount = counts.GetValueOrDefault(dto.Id);

        return dtos;
    }
}
