using Mapster;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Bms.BuildingBlocks.Abstractions.Security;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Equipments;
using EquipmentMaintenanceEntity = Bms.Store.Domain.Entities.EquipmentMaintenance;
using Bms.Store.Infrastructure;

namespace Bms.Store.Application.Services;

/// <summary>
/// 设备维护记录应用服务实现
/// </summary>
public class EquipmentMaintenanceAppService : IEquipmentMaintenanceAppService
{
    private readonly StoreDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<EquipmentMaintenanceCreateDto> _createValidator;
    private readonly IValidator<EquipmentMaintenanceUpdateDto> _updateValidator;

    public EquipmentMaintenanceAppService(
        StoreDbContext dbContext,
        ICurrentUser currentUser,
        IValidator<EquipmentMaintenanceCreateDto> createValidator,
        IValidator<EquipmentMaintenanceUpdateDto> updateValidator)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>
    /// 获取设备维护记录分页列表
    /// </summary>
    public async Task<ApiResponseDto<PagedResponseDto<EquipmentMaintenanceDto>>> GetPagedListAsync(EquipmentMaintenanceQueryDto query)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<PagedResponseDto<EquipmentMaintenanceDto>>.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId ?? 0;
        var queryable = _dbContext.EquipmentMaintenances
            .Where(p => p.TenantId == tenantId && p.StoreId == storeId);

        if (query.EquipmentId.HasValue)
            queryable = queryable.Where(p => p.EquipmentId == query.EquipmentId.Value);
        if (query.MaintenanceType.HasValue)
            queryable = queryable.Where(p => p.MaintenanceType == query.MaintenanceType.Value);
        if (query.StartDate.HasValue)
            queryable = queryable.Where(p => p.MaintenanceDate >= query.StartDate.Value);
        if (query.EndDate.HasValue)
            // EndDate 含当天，加一天作为上界（exclusive）以避免时间部分漏掉当天数据
            queryable = queryable.Where(p => p.MaintenanceDate < query.EndDate.Value.AddDays(1));

        var total = await queryable.CountAsync();
        var items = await queryable
            .OrderByDescending(p => p.CreatedTime)
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        // 批量查询关联设备名称（展示用，按门店隔离）
        var equipmentIds = items.Select(p => p.EquipmentId).Distinct().ToList();
        var equipmentNames = await _dbContext.Equipments
            .Where(e => equipmentIds.Contains(e.Id) && e.StoreId == (_currentUser.StoreId ?? 0))
            .Select(e => new { e.Id, e.Name })
            .ToDictionaryAsync(e => e.Id, e => e.Name);

        var dtos = items.Adapt<List<EquipmentMaintenanceDto>>();
        foreach (var dto in dtos)
        {
            if (equipmentNames.TryGetValue(dto.EquipmentId, out var name))
                dto.EquipmentName = name;
        }

        var result = new PagedResponseDto<EquipmentMaintenanceDto>
        {
            List = dtos,
            Total = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
        return ApiResponseDto<PagedResponseDto<EquipmentMaintenanceDto>>.Ok(result);
    }

    /// <summary>
    /// 根据ID获取设备维护记录详情
    /// </summary>
    public async Task<ApiResponseDto<EquipmentMaintenanceDto?>> GetByIdAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<EquipmentMaintenanceDto?>.Fail("登录状态异常，请重新登录", 401);

        var entity = await _dbContext.EquipmentMaintenances
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == _currentUser.TenantId.Value
                && p.StoreId == (_currentUser.StoreId ?? 0));
        if (entity == null)
            return ApiResponseDto<EquipmentMaintenanceDto?>.Fail("设备维护记录不存在", 404);

        var dto = entity.Adapt<EquipmentMaintenanceDto>();
        // 关联查询设备名称（按门店隔离）
        var equipmentName = await _dbContext.Equipments
            .Where(e => e.Id == entity.EquipmentId && e.StoreId == (_currentUser.StoreId ?? 0))
            .Select(e => e.Name)
            .FirstOrDefaultAsync();
        dto.EquipmentName = equipmentName;
        return ApiResponseDto<EquipmentMaintenanceDto?>.Ok(dto);
    }

    /// <summary>
    /// 创建设备维护记录
    /// 业务规则：
    /// 1) 若 dto 未传 NextMaintenanceDate 且设备配置了 MaintenanceCycleDays，自动按 MaintenanceDate + CycleDays 推算
    /// 2) 同步回写设备的 LastMaintenanceDate 和 NextMaintenanceDate
    /// </summary>
    public async Task<ApiResponseDto<EquipmentMaintenanceDto>> CreateAsync(EquipmentMaintenanceCreateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<EquipmentMaintenanceDto>.Fail("登录状态异常，请重新登录", 401);

        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<EquipmentMaintenanceDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;

        // 校验设备归属当前门店（设备按门店隔离）
        var equipment = await _dbContext.Equipments
            .FirstOrDefaultAsync(e => e.Id == dto.EquipmentId && !e.IsDeleted && e.TenantId == tenantId
                && e.StoreId == (_currentUser.StoreId ?? 0));
        if (equipment == null)
            return ApiResponseDto<EquipmentMaintenanceDto>.Fail("关联设备不存在", 400);

        var entity = dto.Adapt<EquipmentMaintenanceEntity>();
        entity.TenantId = tenantId;
        entity.TenantCode = _currentUser.TenantCode ?? string.Empty;
        // 维护记录为门店级业务数据，记录门店归属（按 StoreId 隔离）
        entity.StoreId = _currentUser.StoreId ?? 0;
        entity.StoreCode = _currentUser.StoreCode ?? string.Empty;
        entity.CreatedTime = DateTime.Now;

        // 自动推算下次保养日期：dto 未传但设备配置了周期
        if (!entity.NextMaintenanceDate.HasValue && equipment.MaintenanceCycleDays.HasValue)
        {
            entity.NextMaintenanceDate = entity.MaintenanceDate.AddDays(equipment.MaintenanceCycleDays.Value);
        }

        _dbContext.EquipmentMaintenances.Add(entity);

        // 同步回写设备日期
        equipment.LastMaintenanceDate = entity.MaintenanceDate;
        if (entity.NextMaintenanceDate.HasValue)
            equipment.NextMaintenanceDate = entity.NextMaintenanceDate;

        // 关闭该设备的未处理保养提醒（按当前租户过滤，避免跨租户误操作）
        var now = DateTime.Now;
        var pendingReminders = await _dbContext.EquipmentMaintenanceReminders
            .Where(r => r.EquipmentId == dto.EquipmentId && r.TenantId == tenantId && !r.IsHandled)
            .ToListAsync();
        foreach (var r in pendingReminders)
        {
            r.IsHandled = true;
            r.HandledTime = now;
            r.UpdatedTime = now;
        }

        await _dbContext.SaveChangesAsync();

        var resultDto = entity.Adapt<EquipmentMaintenanceDto>();
        resultDto.EquipmentName = equipment.Name;
        return ApiResponseDto<EquipmentMaintenanceDto>.Ok(resultDto, "创建成功");
    }

    /// <summary>
    /// 更新设备维护记录
    /// 同步回写设备的 LastMaintenanceDate 和 NextMaintenanceDate
    /// </summary>
    public async Task<ApiResponseDto<EquipmentMaintenanceDto>> UpdateAsync(EquipmentMaintenanceUpdateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<EquipmentMaintenanceDto>.Fail("登录状态异常，请重新登录", 401);

        var validation = await _updateValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<EquipmentMaintenanceDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var entity = await _dbContext.EquipmentMaintenances
            .FirstOrDefaultAsync(p => p.Id == dto.Id && p.TenantId == tenantId
                && p.StoreId == (_currentUser.StoreId ?? 0));
        if (entity == null)
            return ApiResponseDto<EquipmentMaintenanceDto>.Fail("设备维护记录不存在", 404);

        // 设备变更时校验归属
        var equipment = await _dbContext.Equipments
            .FirstOrDefaultAsync(e => e.Id == dto.EquipmentId && !e.IsDeleted && e.TenantId == tenantId);
        if (equipment == null)
            return ApiResponseDto<EquipmentMaintenanceDto>.Fail("关联设备不存在", 400);

        var oldEquipmentId = entity.EquipmentId;
        var newNextMaintenanceDate = dto.NextMaintenanceDate;
        // 若未传下次保养日期且设备有周期，自动推算
        if (!newNextMaintenanceDate.HasValue && equipment.MaintenanceCycleDays.HasValue)
            newNextMaintenanceDate = dto.MaintenanceDate.AddDays(equipment.MaintenanceCycleDays.Value);

        entity.EquipmentId = dto.EquipmentId;
        entity.MaintenanceType = dto.MaintenanceType;
        entity.MaintenanceDate = dto.MaintenanceDate;
        entity.Cost = dto.Cost;
        entity.Operator = dto.Operator;
        entity.Result = dto.Result;
        entity.NextMaintenanceDate = newNextMaintenanceDate;
        entity.Remark = dto.Remark;
        entity.UpdatedTime = DateTime.Now;

        // 同步回写设备日期（仅当本次记录是设备的最新保养时回写才合理，简化处理：直接回写当前关联设备）
        equipment.LastMaintenanceDate = entity.MaintenanceDate;
        if (entity.NextMaintenanceDate.HasValue)
            equipment.NextMaintenanceDate = entity.NextMaintenanceDate;

        await _dbContext.SaveChangesAsync();

        var resultDto = entity.Adapt<EquipmentMaintenanceDto>();
        resultDto.EquipmentName = equipment.Name;
        return ApiResponseDto<EquipmentMaintenanceDto>.Ok(resultDto, "更新成功");
    }

    /// <summary>
    /// 删除设备维护记录（软删除）
    /// </summary>
    public async Task<ApiResponseDto> DeleteAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("登录状态异常，请重新登录", 401);

        var entity = await _dbContext.EquipmentMaintenances
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == _currentUser.TenantId.Value
                && p.StoreId == (_currentUser.StoreId ?? 0));
        if (entity == null)
            return ApiResponseDto.Fail("设备维护记录不存在", 404);

        _dbContext.EquipmentMaintenances.Remove(entity);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, "删除成功");
    }

    /// <summary>
    /// 批量删除设备维护记录（软删除）
    /// </summary>
    public async Task<ApiResponseDto> BatchDeleteAsync(List<long> ids)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("登录状态异常，请重新登录", 401);
        if (ids == null || !ids.Any())
            return ApiResponseDto.Fail("请选择要删除的数据", 400);

        var entities = await _dbContext.EquipmentMaintenances
            .Where(p => ids.Contains(p.Id) && p.TenantId == _currentUser.TenantId.Value
                && p.StoreId == (_currentUser.StoreId ?? 0))
            .ToListAsync();

        _dbContext.EquipmentMaintenances.RemoveRange(entities);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, $"成功删除 {entities.Count} 条数据");
    }
}
