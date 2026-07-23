using Mapster;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Bms.BuildingBlocks.Abstractions.Security;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Appointments;
using Bms.Store.Application.Services.Resources;
using Bms.Store.Domain.Constants;
using AppointmentEntity = Bms.Store.Domain.Entities.Appointment;
using ProductEntity = Bms.Store.Domain.Entities.Product;
using ServiceProductEntity = Bms.Store.Domain.Entities.ServiceProduct;
using Bms.Store.Infrastructure;

namespace Bms.Store.Application.Services;

/// <summary>
/// 预约应用服务实现
/// </summary>
public class AppointmentAppService : IAppointmentAppService
{
    private readonly StoreDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<AppointmentCreateDto> _createValidator;
    private readonly IValidator<AppointmentUpdateDto> _updateValidator;
    private readonly IResourceConflictCheckService _resourceConflictCheckService;

    public AppointmentAppService(
        StoreDbContext dbContext,
        ICurrentUser currentUser,
        IValidator<AppointmentCreateDto> createValidator,
        IValidator<AppointmentUpdateDto> updateValidator,
        IResourceConflictCheckService resourceConflictCheckService)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _resourceConflictCheckService = resourceConflictCheckService;
    }

    /// <summary>
    /// 获取预约分页列表
    /// </summary>
    public async Task<ApiResponseDto<PagedResponseDto<AppointmentDto>>> GetPagedListAsync(AppointmentQueryDto query)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<PagedResponseDto<AppointmentDto>>.Fail("无法确定当前租户", 401);

        var tenantId = _currentUser.TenantId.Value;
        var queryable = _dbContext.Appointments
            .Where(a => a.TenantId == tenantId);

        if (query.CustomerId.HasValue)
            queryable = queryable.Where(a => a.CustomerId == query.CustomerId.Value);
        if (query.Status.HasValue)
            queryable = queryable.Where(a => a.Status == query.Status.Value);
        if (query.AppointmentDateStart.HasValue)
            queryable = queryable.Where(a => a.AppointmentDate >= query.AppointmentDateStart.Value);
        if (query.AppointmentDateEnd.HasValue)
            queryable = queryable.Where(a => a.AppointmentDate <= query.AppointmentDateEnd.Value);
        if (query.TechnicianSource.HasValue)
        {
            var matchingTechIds = _dbContext.Technicians
                .Where(t => t.Source == query.TechnicianSource.Value)
                .Select(t => t.Id);
            queryable = queryable.Where(a => a.TechnicianId.HasValue && matchingTechIds.Contains(a.TechnicianId.Value));
        }

        var total = await queryable.CountAsync();
        var items = await queryable
            .OrderByDescending(a => a.CreatedTime)
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        // 查询关联技师的来源，填充到 DTO
        var technicianIds = items.Where(a => a.TechnicianId.HasValue).Select(a => a.TechnicianId!.Value).Distinct().ToList();
        var technicianSources = await _dbContext.Technicians
            .Where(t => technicianIds.Contains(t.Id))
            .Select(t => new { t.Id, t.Source })
            .ToListAsync();

        // 批量查询关联商品名称，填充到 DTO（替代原 ServiceItem 字符串字段）
        var productIds = items.Select(a => a.ProductId).Distinct().ToList();
        var productNames = await _dbContext.Products
            .Where(p => productIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Name })
            .ToListAsync();

        var dtos = items.Adapt<List<AppointmentDto>>();
        foreach (var dto in dtos)
        {
            if (dto.TechnicianId.HasValue)
            {
                var tech = technicianSources.FirstOrDefault(t => t.Id == dto.TechnicianId.Value);
                dto.TechnicianSource = tech?.Source;
            }
            dto.ProductName = productNames.FirstOrDefault(p => p.Id == dto.ProductId)?.Name;
        }

        var result = new PagedResponseDto<AppointmentDto>
        {
            List = dtos,
            Total = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
        return ApiResponseDto<PagedResponseDto<AppointmentDto>>.Ok(result);
    }

    /// <summary>
    /// 根据ID获取预约详情
    /// </summary>
    public async Task<ApiResponseDto<AppointmentDto?>> GetByIdAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<AppointmentDto?>.Fail("无法确定当前租户", 401);

        var entity = await _dbContext.Appointments
            .FirstOrDefaultAsync(a => a.Id == id && a.TenantId == _currentUser.TenantId.Value);
        if (entity == null)
            return ApiResponseDto<AppointmentDto?>.Fail("预约不存在", 404);

        var dto = entity.Adapt<AppointmentDto>();
        if (entity.TechnicianId.HasValue)
        {
            var tech = await _dbContext.Technicians
                .FirstOrDefaultAsync(t => t.Id == entity.TechnicianId.Value);
            dto.TechnicianSource = tech?.Source;
        }

        // 填充服务项目商品名称（替代原 ServiceItem 字符串字段）
        var product = await _dbContext.Products
            .FirstOrDefaultAsync(p => p.Id == entity.ProductId);
        dto.ProductName = product?.Name;

        return ApiResponseDto<AppointmentDto?>.Ok(dto);
    }

    /// <summary>
    /// 创建预约
    /// EndTime 由后端根据 ProductId 关联的 ServiceProduct.Duration 权威计算，前端无需传入
    /// </summary>
    public async Task<ApiResponseDto<AppointmentDto>> CreateAsync(AppointmentCreateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<AppointmentDto>.Fail("无法确定当前租户", 401);

        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<AppointmentDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;

        // 检查预约号唯一性
        var noExists = await _dbContext.Appointments
            .AnyAsync(a => a.AppointmentNo == dto.AppointmentNo && a.TenantId == tenantId );
        if (noExists)
            return ApiResponseDto<AppointmentDto>.Fail($"预约号 {dto.AppointmentNo} 已存在", 400);

        // 查询服务项目商品（校验 type=2 服务项目）+ ServiceProduct（获取 Duration）
        var (product, serviceProduct) = await GetServiceProductAsync(dto.ProductId, tenantId);
        if (product == null)
            return ApiResponseDto<AppointmentDto>.Fail("服务项目不存在或非服务项目类型", 400);
        if (serviceProduct?.Duration == null)
            return ApiResponseDto<AppointmentDto>.Fail("服务项目未配置服务时长", 400);

        // 校验所选房间符合服务项目所需房间类型（RequiredRoomType）
        if (serviceProduct.RequiredRoomType.HasValue && dto.RoomId.HasValue)
        {
            var room = await _dbContext.Rooms
                .FirstOrDefaultAsync(r => r.Id == dto.RoomId.Value && !r.IsDeleted && r.TenantId == tenantId);
            if (room == null)
                return ApiResponseDto<AppointmentDto>.Fail("所选房间不存在", 400);
            if (room.RoomType != serviceProduct.RequiredRoomType.Value)
                return ApiResponseDto<AppointmentDto>.Fail(
                    $"所选房间类型({room.RoomType})不符合服务项目所需房间类型({serviceProduct.RequiredRoomType.Value})", 400);
        }

        // 校验所选设备类型符合服务项目要求（ServiceProductEquipment 关联表）
        // 服务项目未关联任何设备类型时跳过校验（无设备要求）；关联后所选设备的 EquipmentTypeId 必须在允许列表内
        if (dto.EquipmentId.HasValue)
        {
            var equipment = await _dbContext.Equipments
                .FirstOrDefaultAsync(e => e.Id == dto.EquipmentId.Value && !e.IsDeleted && e.TenantId == tenantId);
            if (equipment == null)
                return ApiResponseDto<AppointmentDto>.Fail("所选设备不存在", 400);
            // 设备状态：1=正常 可用；2=维修中 3=已停用 不可用
            if (equipment.Status != EquipmentStatus.Normal)
                return ApiResponseDto<AppointmentDto>.Fail($"所选设备当前状态({EquipmentStatus.GetName(equipment.Status)})不可用", 400);

            var allowedTypeIds = await _dbContext.ServiceProductEquipments
                .Where(spe => spe.ServiceProductId == serviceProduct.Id && spe.TenantId == tenantId)
                .Select(spe => spe.EquipmentTypeId)
                .ToListAsync();
            if (allowedTypeIds.Count > 0 && !allowedTypeIds.Contains(equipment.EquipmentTypeId))
                return ApiResponseDto<AppointmentDto>.Fail("所选设备类型不符合服务项目要求", 400);
        }

        // 计算 EndTime（基于 ServiceProduct.Duration，后端权威计算）
        var start = dto.AppointmentDate.Date.Add(dto.AppointmentTime);
        var endTime = start.AddMinutes(serviceProduct.Duration.Value);

        // 检查资源冲突（技师/房间/设备任一相同且时段重叠即冲突，跨 Appointment + OrderItem 双向）
        var conflictMsg = await BuildConflictMessageAsync(
            tenantId, dto.TechnicianId, dto.RoomId, dto.EquipmentId,
            start, endTime);
        if (conflictMsg != null)
            return ApiResponseDto<AppointmentDto>.Fail(conflictMsg, 400);

        var entity = dto.Adapt<AppointmentEntity>();
        entity.TenantId = tenantId;
        entity.TenantCode = _currentUser.TenantCode ?? string.Empty;
        entity.EndTime = endTime;
        entity.CreatedTime = DateTime.Now;

        // 自动填充 TechnicianSource：根据 Technician.Source（1=自有，2=平台）
        // 不按 TenantId 过滤，因平台技师属于平台租户，需跨租户查询
        if (dto.TechnicianId.HasValue)
        {
            var technician = await _dbContext.Technicians
                .FirstOrDefaultAsync(t => t.Id == dto.TechnicianId.Value);
            if (technician != null)
                entity.TechnicianSource = technician.Source;
        }

        _dbContext.Appointments.Add(entity);
        await _dbContext.SaveChangesAsync();

        // 填充 ProductName 返回
        var resultDto = entity.Adapt<AppointmentDto>();
        resultDto.ProductName = product.Name;
        return ApiResponseDto<AppointmentDto>.Ok(resultDto, "创建成功");
    }

    /// <summary>
    /// 更新预约
    /// EndTime 由后端根据 ProductId 关联的 ServiceProduct.Duration 权威计算，前端无需传入
    /// </summary>
    public async Task<ApiResponseDto<AppointmentDto>> UpdateAsync(AppointmentUpdateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<AppointmentDto>.Fail("无法确定当前租户", 401);

        var validation = await _updateValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<AppointmentDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var entity = await _dbContext.Appointments
            .FirstOrDefaultAsync(a => a.Id == dto.Id && a.TenantId == tenantId);
        if (entity == null)
            return ApiResponseDto<AppointmentDto>.Fail("预约不存在", 404);

        // 预约号变更时检查唯一性
        if (entity.AppointmentNo != dto.AppointmentNo)
        {
            var noExists = await _dbContext.Appointments
                .AnyAsync(a => a.AppointmentNo == dto.AppointmentNo && a.TenantId == tenantId && a.Id != dto.Id);
            if (noExists)
                return ApiResponseDto<AppointmentDto>.Fail($"预约号 {dto.AppointmentNo} 已存在", 400);
        }

        // 查询服务项目商品（校验 type=2 服务项目）+ ServiceProduct（获取 Duration）
        var (product, serviceProduct) = await GetServiceProductAsync(dto.ProductId, tenantId);
        if (product == null)
            return ApiResponseDto<AppointmentDto>.Fail("服务项目不存在或非服务项目类型", 400);
        if (serviceProduct?.Duration == null)
            return ApiResponseDto<AppointmentDto>.Fail("服务项目未配置服务时长", 400);

        // 校验所选房间符合服务项目所需房间类型（RequiredRoomType）
        if (serviceProduct.RequiredRoomType.HasValue && dto.RoomId.HasValue)
        {
            var room = await _dbContext.Rooms
                .FirstOrDefaultAsync(r => r.Id == dto.RoomId.Value && !r.IsDeleted && r.TenantId == tenantId);
            if (room == null)
                return ApiResponseDto<AppointmentDto>.Fail("所选房间不存在", 400);
            if (room.RoomType != serviceProduct.RequiredRoomType.Value)
                return ApiResponseDto<AppointmentDto>.Fail(
                    $"所选房间类型({room.RoomType})不符合服务项目所需房间类型({serviceProduct.RequiredRoomType.Value})", 400);
        }

        // 校验所选设备类型符合服务项目要求（ServiceProductEquipment 关联表）
        // 服务项目未关联任何设备类型时跳过校验（无设备要求）；关联后所选设备的 EquipmentTypeId 必须在允许列表内
        if (dto.EquipmentId.HasValue)
        {
            var equipment = await _dbContext.Equipments
                .FirstOrDefaultAsync(e => e.Id == dto.EquipmentId.Value && !e.IsDeleted && e.TenantId == tenantId);
            if (equipment == null)
                return ApiResponseDto<AppointmentDto>.Fail("所选设备不存在", 400);
            // 设备状态：1=正常 可用；2=维修中 3=已停用 不可用
            if (equipment.Status != EquipmentStatus.Normal)
                return ApiResponseDto<AppointmentDto>.Fail($"所选设备当前状态({EquipmentStatus.GetName(equipment.Status)})不可用", 400);

            var allowedTypeIds = await _dbContext.ServiceProductEquipments
                .Where(spe => spe.ServiceProductId == serviceProduct.Id && spe.TenantId == tenantId)
                .Select(spe => spe.EquipmentTypeId)
                .ToListAsync();
            if (allowedTypeIds.Count > 0 && !allowedTypeIds.Contains(equipment.EquipmentTypeId))
                return ApiResponseDto<AppointmentDto>.Fail("所选设备类型不符合服务项目要求", 400);
        }

        // 计算 EndTime（基于 ServiceProduct.Duration，后端权威计算）
        var start = dto.AppointmentDate.Date.Add(dto.AppointmentTime);
        var endTime = start.AddMinutes(serviceProduct.Duration.Value);

        // 检查资源冲突（技师/房间/设备任一相同且时段重叠即冲突，跨 Appointment + OrderItem 双向，排除自身）
        var conflictMsg = await BuildConflictMessageAsync(
            tenantId, dto.TechnicianId, dto.RoomId, dto.EquipmentId,
            start, endTime, excludeAppointmentId: dto.Id);
        if (conflictMsg != null)
            return ApiResponseDto<AppointmentDto>.Fail(conflictMsg, 400);

        entity.AppointmentNo = dto.AppointmentNo;
        entity.CustomerId = dto.CustomerId;
        entity.CustomerName = dto.CustomerName;
        entity.CustomerPhone = dto.CustomerPhone;
        entity.AppointmentDate = dto.AppointmentDate;
        entity.AppointmentTime = dto.AppointmentTime;
        entity.EndTime = endTime;

        // 状态流转校验：非法流转返回 400 并提示允许的下一状态
        if (entity.Status != dto.Status)
        {
            if (!AppointmentStatusTransition.CanTransition(entity.Status, dto.Status))
            {
                var allowed = AppointmentStatusTransition.GetAllowedTransitions(entity.Status);
                var allowedText = allowed.Any()
                    ? string.Join(",", allowed)
                    : "无（当前为终态，不可流转）";
                return ApiResponseDto<AppointmentDto>.Fail(
                    $"预约状态 {entity.Status} 不能流转到 {dto.Status}，允许的状态：{allowedText}", 400);
            }
        }

        entity.Status = dto.Status;
        entity.TechnicianId = dto.TechnicianId;
        // 技师变更时同步更新 TechnicianSource，保持与 Technician.Source 一致
        if (dto.TechnicianId.HasValue)
        {
            var technician = await _dbContext.Technicians
                .FirstOrDefaultAsync(t => t.Id == dto.TechnicianId.Value);
            entity.TechnicianSource = technician?.Source;
        }
        else
        {
            entity.TechnicianSource = null;
        }
        entity.RoomId = dto.RoomId;
        entity.EquipmentId = dto.EquipmentId;
        entity.ProductId = dto.ProductId;
        entity.Remark = dto.Remark;
        entity.ConfirmTime = dto.ConfirmTime;
        entity.ArrivalTime = dto.ArrivalTime;
        entity.CompleteTime = dto.CompleteTime;
        entity.UpdatedTime = DateTime.Now;

        await _dbContext.SaveChangesAsync();

        var resultDto = entity.Adapt<AppointmentDto>();
        resultDto.ProductName = product.Name;
        return ApiResponseDto<AppointmentDto>.Ok(resultDto, "更新成功");
    }

    /// <summary>
    /// 检查预约资源冲突并返回用户可读提示信息
    /// 委托 IResourceConflictCheckService 跨 Appointment + OrderItem 双向检测
    /// 状态过滤：Appointment.Status IN (2已预约, 3已到店) / Order.Status = 1进行中
    /// </summary>
    /// <param name="start">预约开始时间（由调用方根据 AppointmentDate + AppointmentTime 计算）</param>
    /// <param name="end">预约结束时间（由调用方根据 ServiceProduct.Duration 计算）</param>
    /// <returns>冲突提示信息;无冲突返回 null</returns>
    private async Task<string?> BuildConflictMessageAsync(
        long tenantId,
        long? technicianId,
        long? roomId,
        long? equipmentId,
        DateTime start,
        DateTime end,
        long? excludeAppointmentId = null)
    {
        var result = await _resourceConflictCheckService.CheckAsync(
            tenantId, technicianId, roomId, equipmentId,
            start, end, excludeAppointmentId);

        if (!result.HasAnyConflict)
            return null;

        // 拼接用户可读的冲突提示
        var parts = new List<string>();
        if (result.TechnicianConflict)
            parts.Add($"技师（{result.TechnicianConflictInfo}）");
        if (result.RoomConflict)
            parts.Add($"房间（{result.RoomConflictInfo}）");
        if (result.EquipmentConflict)
            parts.Add($"设备（{result.EquipmentConflictInfo}）");

        return $"{string.Join("；", parts)}已被占用，请更换时段或资源";
    }

    /// <summary>
    /// 查询服务项目商品及其 ServiceProduct 子表
    /// 校验：商品存在 + 归属当前租户 + type=2 服务项目
    /// </summary>
    /// <returns>(Product, ServiceProduct)；若商品不存在或类型不对返回 (null, null)</returns>
    private async Task<(ProductEntity? Product, ServiceProductEntity? ServiceProduct)> GetServiceProductAsync(long productId, long tenantId)
    {
        var product = await _dbContext.Products
            .FirstOrDefaultAsync(p => p.Id == productId && p.TenantId == tenantId && p.Type == 2);
        if (product == null)
            return (null, null);

        var serviceProduct = await _dbContext.ServiceProducts
            .FirstOrDefaultAsync(sp => sp.ProductId == productId && sp.TenantId == tenantId);
        return (product, serviceProduct);
    }

    /// <summary>
    /// 删除预约
    /// </summary>
    public async Task<ApiResponseDto> DeleteAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("无法确定当前租户", 401);

        var entity = await _dbContext.Appointments
            .FirstOrDefaultAsync(a => a.Id == id && a.TenantId == _currentUser.TenantId.Value);
        if (entity == null)
            return ApiResponseDto.Fail("预约不存在", 404);

        _dbContext.Appointments.Remove(entity);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, "删除成功");
    }

    /// <summary>
    /// 批量删除预约
    /// </summary>
    public async Task<ApiResponseDto> BatchDeleteAsync(List<long> ids)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("无法确定当前租户", 401);
        if (ids == null || !ids.Any())
            return ApiResponseDto.Fail("请选择要删除的数据", 400);

        var entities = await _dbContext.Appointments
            .Where(a => ids.Contains(a.Id) && a.TenantId == _currentUser.TenantId.Value)
            .ToListAsync();

        _dbContext.Appointments.RemoveRange(entities);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, $"成功删除 {entities.Count} 条数据");
    }

    /// <summary>
    /// 获取明日预约提醒分页列表
    /// 查询条件：AppointmentDate=明天 AND Status IN(1待确认,2已确认)
    /// </summary>
    public async Task<ApiResponseDto<PagedResponseDto<TomorrowReminderDto>>> GetTomorrowRemindersAsync(TomorrowReminderQueryDto query)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<PagedResponseDto<TomorrowReminderDto>>.Fail("无法确定当前租户", 401);

        var tenantId = _currentUser.TenantId.Value;
        // 明天的日期范围（避免 DateTime 比较时的时间部分干扰）
        var tomorrow = DateTime.Today.AddDays(1);
        var dayAfterTomorrow = tomorrow.AddDays(1);

        var queryable = _dbContext.Appointments
            .Where(a => a.TenantId == tenantId
                && a.AppointmentDate >= tomorrow
                && a.AppointmentDate < dayAfterTomorrow
                && (a.Status == 1 || a.Status == 2));

        if (!string.IsNullOrWhiteSpace(query.CustomerName))
            queryable = queryable.Where(a => a.CustomerName.Contains(query.CustomerName));
        if (!string.IsNullOrWhiteSpace(query.Phone))
            queryable = queryable.Where(a => a.CustomerPhone.Contains(query.Phone));
        if (query.RemindStatus.HasValue)
            queryable = queryable.Where(a => a.ReminderStatus == query.RemindStatus.Value);

        var total = await queryable.CountAsync();
        var items = await queryable
            .OrderBy(a => a.AppointmentTime)
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        // 批量查询关联技师名称，避免 N+1
        var technicianIds = items.Where(a => a.TechnicianId.HasValue).Select(a => a.TechnicianId!.Value).Distinct().ToList();
        var technicianNames = await _dbContext.Technicians
            .Where(t => technicianIds.Contains(t.Id))
            .Select(t => new { t.Id, t.Name })
            .ToListAsync();

        // 批量查询关联商品名称，避免 N+1（替代原 ServiceItem 字符串字段）
        var productIds = items.Select(a => a.ProductId).Distinct().ToList();
        var productNames = await _dbContext.Products
            .Where(p => productIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Name })
            .ToDictionaryAsync(p => p.Id, p => p.Name);

        var dtos = items.Select(a => new TomorrowReminderDto
        {
            Id = a.Id,
            AppointmentNo = a.AppointmentNo,
            CustomerName = a.CustomerName,
            Phone = a.CustomerPhone,
            ServiceName = productNames.TryGetValue(a.ProductId, out var name) ? name : string.Empty,
            TechnicianName = a.TechnicianId.HasValue
                ? technicianNames.FirstOrDefault(t => t.Id == a.TechnicianId.Value)?.Name
                : null,
            AppointmentTime = $"{a.AppointmentDate:yyyy-MM-dd} {a.AppointmentTime:hh\\:mm}",
            RemindStatus = a.ReminderStatus,
            CustomerConfirmStatus = MapCustomerConfirmStatus(a.Status),
            Remark = a.Remark
        }).ToList();

        var result = new PagedResponseDto<TomorrowReminderDto>
        {
            List = dtos,
            Total = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
        return ApiResponseDto<PagedResponseDto<TomorrowReminderDto>>.Ok(result);
    }

    /// <summary>
    /// 发送提醒（更新提醒状态为已提醒，记录提醒时间）
    /// </summary>
    public async Task<ApiResponseDto> SendReminderAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("无法确定当前租户", 401);

        var entity = await _dbContext.Appointments
            .FirstOrDefaultAsync(a => a.Id == id && a.TenantId == _currentUser.TenantId.Value);
        if (entity == null)
            return ApiResponseDto.Fail("预约不存在", 404);

        entity.ReminderStatus = 2;
        entity.ReminderTime = DateTime.Now;
        entity.UpdatedTime = DateTime.Now;

        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, "提醒已发送");
    }

    /// <summary>
    /// 确认明日预约（更新状态为已确认，记录确认时间）
    /// </summary>
    public async Task<ApiResponseDto> ConfirmTomorrowAppointmentAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("无法确定当前租户", 401);

        var entity = await _dbContext.Appointments
            .FirstOrDefaultAsync(a => a.Id == id && a.TenantId == _currentUser.TenantId.Value);
        if (entity == null)
            return ApiResponseDto.Fail("预约不存在", 404);

        entity.Status = 2;
        entity.ConfirmTime = DateTime.Now;
        entity.UpdatedTime = DateTime.Now;

        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, "预约已确认");
    }

    /// <summary>
    /// 预约状态映射为客户确认状态
    /// 1待确认->1, 2已预约->2, 5已取消->3需改期
    /// </summary>
    private static int MapCustomerConfirmStatus(int status) => status switch
    {
        1 => 1,
        2 => 2,
        5 => 3,
        _ => 1
    };
}
