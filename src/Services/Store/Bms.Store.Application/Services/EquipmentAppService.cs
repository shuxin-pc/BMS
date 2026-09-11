using Mapster;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Bms.BuildingBlocks.Abstractions.Security;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Equipments;
using Bms.Store.Domain.Constants;
using Bms.Store.Domain.Entities;
using EquipmentEntity = Bms.Store.Domain.Entities.Equipment;
using Bms.Store.Infrastructure;

namespace Bms.Store.Application.Services;

/// <summary>
/// 设备台账应用服务实现
/// </summary>
public class EquipmentAppService : IEquipmentAppService
{
    private readonly StoreDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<EquipmentCreateDto> _createValidator;
    private readonly IValidator<EquipmentUpdateDto> _updateValidator;

    public EquipmentAppService(
        StoreDbContext dbContext,
        ICurrentUser currentUser,
        IValidator<EquipmentCreateDto> createValidator,
        IValidator<EquipmentUpdateDto> updateValidator)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<ApiResponseDto<PagedResponseDto<EquipmentDto>>> GetPagedListAsync(EquipmentQueryDto query)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<PagedResponseDto<EquipmentDto>>.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId ?? 0;
        var queryable = _dbContext.Equipments
            .Where(e => !e.IsDeleted && e.TenantId == tenantId && e.StoreId == storeId);

        if (!string.IsNullOrWhiteSpace(query.Name))
            queryable = queryable.Where(e => e.Name.Contains(query.Name));
        if (!string.IsNullOrWhiteSpace(query.Code))
            queryable = queryable.Where(e => e.Code.Contains(query.Code));
        if (query.Status.HasValue)
            queryable = queryable.Where(e => e.Status == query.Status.Value);

        var total = await queryable.CountAsync();
        var items = await queryable
            .OrderByDescending(e => e.CreatedTime)
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var result = new PagedResponseDto<EquipmentDto>
        {
            List = await FillEquipmentTypeNamesAsync(items.Adapt<List<EquipmentDto>>(), tenantId),
            Total = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
        return ApiResponseDto<PagedResponseDto<EquipmentDto>>.Ok(result);
    }

    public async Task<ApiResponseDto<EquipmentDto?>> GetByIdAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<EquipmentDto?>.Fail("登录状态异常，请重新登录", 401);

        var equipment = await _dbContext.Equipments
            .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted && e.TenantId == _currentUser.TenantId.Value
                && e.StoreId == (_currentUser.StoreId ?? 0));
        if (equipment == null)
            return ApiResponseDto<EquipmentDto?>.Fail("设备不存在", 404);
        var dto = equipment.Adapt<EquipmentDto>();
        await FillEquipmentTypeNamesAsync(new List<EquipmentDto> { dto }, _currentUser.TenantId.Value);
        return ApiResponseDto<EquipmentDto?>.Ok(dto);
    }

    public async Task<ApiResponseDto<EquipmentDto>> CreateAsync(EquipmentCreateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<EquipmentDto>.Fail("登录状态异常，请重新登录", 401);

        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<EquipmentDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId ?? 0;
        var codeExists = await _dbContext.Equipments
            .AnyAsync(e => e.Code == dto.Code && e.TenantId == tenantId && e.StoreId == storeId && !e.IsDeleted);
        if (codeExists)
            return ApiResponseDto<EquipmentDto>.Fail($"编码 {dto.Code} 已存在", 400);

        var typeExists = await _dbContext.EquipmentTypes
            .AnyAsync(t => t.Id == dto.EquipmentTypeId && t.TenantId == tenantId && !t.IsDeleted);
        if (!typeExists)
            return ApiResponseDto<EquipmentDto>.Fail("设备类型不存在或已被删除", 400);

        // 停用类型不可用于新的设备档案挂接
        if (!await _dbContext.EquipmentTypes
                .AnyAsync(t => t.Id == dto.EquipmentTypeId && t.TenantId == tenantId && !t.IsDeleted && t.IsActive))
            return ApiResponseDto<EquipmentDto>.Fail("设备类型已停用，请先启用后再挂接设备", 400);

        var equipment = dto.Adapt<EquipmentEntity>();
        equipment.TenantId = tenantId;
        equipment.TenantCode = _currentUser.TenantCode ?? string.Empty;
        // 设备为门店级数据，记录门店归属（按 StoreId 隔离）
        equipment.StoreId = storeId;
        equipment.StoreCode = _currentUser.StoreCode ?? string.Empty;
        equipment.CreatedTime = DateTime.Now;

        _dbContext.Equipments.Add(equipment);
        await _dbContext.SaveChangesAsync();

        return ApiResponseDto<EquipmentDto>.Ok(equipment.Adapt<EquipmentDto>(), "创建成功");
    }

    public async Task<ApiResponseDto<EquipmentDto>> UpdateAsync(EquipmentUpdateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<EquipmentDto>.Fail("登录状态异常，请重新登录", 401);

        var validation = await _updateValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<EquipmentDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var equipment = await _dbContext.Equipments
            .FirstOrDefaultAsync(e => e.Id == dto.Id && !e.IsDeleted && e.TenantId == tenantId
                && e.StoreId == (_currentUser.StoreId ?? 0));
        if (equipment == null)
            return ApiResponseDto<EquipmentDto>.Fail("设备不存在", 404);

        if (equipment.Code != dto.Code)
        {
            var codeExists = await _dbContext.Equipments
                .AnyAsync(e => e.Code == dto.Code && e.TenantId == tenantId && e.StoreId == (_currentUser.StoreId ?? 0)
                    && !e.IsDeleted && e.Id != dto.Id);
            if (codeExists)
                return ApiResponseDto<EquipmentDto>.Fail($"编码 {dto.Code} 已存在", 400);
        }

        var typeExists = await _dbContext.EquipmentTypes
            .AnyAsync(t => t.Id == dto.EquipmentTypeId && t.TenantId == tenantId && !t.IsDeleted);
        if (!typeExists)
            return ApiResponseDto<EquipmentDto>.Fail("设备类型不存在或已被删除", 400);

        // 停用类型不可用于新的设备档案挂接
        if (!await _dbContext.EquipmentTypes
                .AnyAsync(t => t.Id == dto.EquipmentTypeId && t.TenantId == tenantId && !t.IsDeleted && t.IsActive))
            return ApiResponseDto<EquipmentDto>.Fail("设备类型已停用，请先启用后再挂接设备", 400);

        equipment.EquipmentTypeId = dto.EquipmentTypeId;
        equipment.Name = dto.Name;
        equipment.Code = dto.Code;
        equipment.Model = dto.Model;
        equipment.Manufacturer = dto.Manufacturer;
        equipment.PurchaseDate = dto.PurchaseDate;
        equipment.PurchasePrice = dto.PurchasePrice;
        equipment.Status = dto.Status;
        equipment.Location = dto.Location;
        equipment.LastMaintenanceDate = dto.LastMaintenanceDate;
        equipment.NextMaintenanceDate = dto.NextMaintenanceDate;
        equipment.MaintenanceCycleDays = dto.MaintenanceCycleDays;
        equipment.Remark = dto.Remark;
        equipment.UpdatedTime = DateTime.Now;

        await _dbContext.SaveChangesAsync();
        return ApiResponseDto<EquipmentDto>.Ok(equipment.Adapt<EquipmentDto>(), "更新成功");
    }

    public async Task<ApiResponseDto> DeleteAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("登录状态异常，请重新登录", 401);

        var equipment = await _dbContext.Equipments
            .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted && e.TenantId == _currentUser.TenantId.Value
                && e.StoreId == (_currentUser.StoreId ?? 0));
        if (equipment == null)
            return ApiResponseDto.Fail("设备不存在", 404);

        equipment.IsDeleted = true;
        equipment.UpdatedTime = DateTime.Now;
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, "删除成功");
    }

    public async Task<ApiResponseDto> BatchDeleteAsync(List<long> ids)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("登录状态异常，请重新登录", 401);
        if (ids == null || !ids.Any())
            return ApiResponseDto.Fail("请选择要删除的数据", 400);

        var equipments = await _dbContext.Equipments
            .Where(e => ids.Contains(e.Id) && !e.IsDeleted && e.TenantId == _currentUser.TenantId.Value
                && e.StoreId == (_currentUser.StoreId ?? 0))
            .ToListAsync();

        foreach (var equipment in equipments)
        {
            equipment.IsDeleted = true;
            equipment.UpdatedTime = DateTime.Now;
        }
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, $"成功删除 {equipments.Count} 条数据");
    }

    /// <summary>
    /// 查询即将到期保养的设备列表
    /// 规则：NextMaintenanceDate 非空 且 <= 今天 + days（含已过期未保养）
    /// 排序：按 NextMaintenanceDate 升序（最紧急的在前）
    /// </summary>
    public async Task<ApiResponseDto<List<EquipmentDto>>> GetUpcomingMaintenanceAsync(int days = 7)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<List<EquipmentDto>>.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;
        var threshold = DateTime.Today.AddDays(days);

        var items = await _dbContext.Equipments
            .Where(e => !e.IsDeleted && e.TenantId == tenantId && e.StoreId == (_currentUser.StoreId ?? 0)
                && e.NextMaintenanceDate.HasValue
                && e.NextMaintenanceDate.Value <= threshold)
            .OrderBy(e => e.NextMaintenanceDate)
            .ToListAsync();

        return ApiResponseDto<List<EquipmentDto>>.Ok(await FillEquipmentTypeNamesAsync(items.Adapt<List<EquipmentDto>>(), tenantId));
    }

    /// <summary>
    /// 根据服务项目查询可用设备列表
    /// ① 服务项目页语境：通过商品主档ID（masterId）反查租户内 ServiceProduct（租户内 MasterId 唯一，1:1），
    ///    serviceProductId 与 masterId 均未提供时返回空列表（无设备要求）
    /// ② 查询服务项目关联的设备类型列表（ServiceProductEquipment 多对多关联）
    /// ③ 按允许的设备类型过滤本租户正常状态设备（Status = 1 正常；2 维修中、3 已停用 不可用）
    /// ④ 排除指定时段已冲突的设备（预约表：状态非已取消/已完成，时段重叠）
    /// 服务项目未关联任何设备类型时返回空列表（表示无设备要求）
    /// </summary>
    public async Task<ApiResponseDto<List<EquipmentDto>>> GetAvailableByServiceProductAsync(
        long? serviceProductId,
        long? masterId,
        DateTime? startTime,
        DateTime? endTime,
        long? excludeAppointmentId = null)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<List<EquipmentDto>>.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;

        // 服务项目页语境：通过商品主档ID反查租户内 ServiceProduct（租户内 MasterId 唯一，1:1）
        if (masterId.HasValue)
        {
            serviceProductId = await _dbContext.ServiceProducts
                .Where(s => !s.IsDeleted && s.TenantId == tenantId && s.MasterId == masterId.Value)
                .Select(s => (long?)s.Id)
                .FirstOrDefaultAsync();
        }

        // serviceProductId 与 masterId 均未提供，或 masterId 反查无匹配时，视为无设备要求
        if (!serviceProductId.HasValue)
            return ApiResponseDto<List<EquipmentDto>>.Ok(new List<EquipmentDto>());

        // 查询服务项目允许的设备类型列表
        var allowedTypeIds = await _dbContext.ServiceProductEquipments
            .Where(spe => spe.ServiceProductId == serviceProductId.Value && spe.TenantId == tenantId)
            .Select(spe => spe.EquipmentTypeId)
            .ToListAsync();
        if (allowedTypeIds.Count == 0)
            return ApiResponseDto<List<EquipmentDto>>.Ok(new List<EquipmentDto>());

        // 查询本门店正常状态设备（按允许的设备类型过滤；维修中/已停用不可用）
        var equipments = await _dbContext.Equipments
            .Where(e => !e.IsDeleted && e.TenantId == tenantId && e.StoreId == (_currentUser.StoreId ?? 0)
                && e.Status == EquipmentStatus.Normal
                && allowedTypeIds.Contains(e.EquipmentTypeId))
            .ToListAsync();

        // 未提供时段时仅按类型过滤返回，不做冲突排除（时段确定后才校验冲突）
        if (!startTime.HasValue || !endTime.HasValue)
            return ApiResponseDto<List<EquipmentDto>>.Ok(await FillEquipmentTypeNamesAsync(equipments.Adapt<List<EquipmentDto>>(), tenantId));

        var start = startTime.Value;
        var end = endTime.Value;

        // 排除时段冲突的设备（预约表：状态非已完成/已取消/爽约，时段重叠）
        var startDate = start.Date;
        var endDate = end.Date;
        var conflictCandidates = await _dbContext.Appointments
            .Where(a => a.TenantId == tenantId && a.StoreId == (_currentUser.StoreId ?? 0)
                && a.Status != AppointmentStatus.Completed
                && a.Status != AppointmentStatus.Cancelled
                && a.Status != AppointmentStatus.NoShow
                && a.EquipmentId.HasValue
                && (excludeAppointmentId == null || a.Id != excludeAppointmentId.Value)
                && a.StartTime.Date >= startDate && a.StartTime.Date <= endDate)
            .Select(a => new { a.EquipmentId, a.StartTime, a.EndTime })
            .ToListAsync();

        var conflictEquipmentIds = conflictCandidates
            .Where(a => a.EndTime.HasValue
                && a.StartTime < end
                && a.EndTime.Value > start)
            .Select(a => a.EquipmentId!.Value)
            .Distinct()
            .ToList();

        var available = equipments.Where(e => !conflictEquipmentIds.Contains(e.Id)).ToList();
        return ApiResponseDto<List<EquipmentDto>>.Ok(await FillEquipmentTypeNamesAsync(available.Adapt<List<EquipmentDto>>(), tenantId));
    }

    /// <summary>
    /// 批量填充设备列表的类型名称（设备类型为租户级共享数据）
    /// </summary>
    private async Task<List<EquipmentDto>> FillEquipmentTypeNamesAsync(List<EquipmentDto> dtos, long tenantId)
    {
        var typeIds = dtos.Select(d => d.EquipmentTypeId).Distinct().ToList();
        if (typeIds.Count == 0)
            return dtos;

        var typeMap = await _dbContext.EquipmentTypes
            .Where(t => typeIds.Contains(t.Id) && t.TenantId == tenantId && !t.IsDeleted)
            .ToDictionaryAsync(t => t.Id, t => t.Name);

        foreach (var dto in dtos)
            dto.EquipmentTypeName = typeMap.GetValueOrDefault(dto.EquipmentTypeId);

        return dtos;
    }
}
