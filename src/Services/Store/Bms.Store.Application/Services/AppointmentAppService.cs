using Mapster;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Bms.BuildingBlocks.Abstractions.Security;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Appointments;
using Bms.Store.Application.Services.Resources;
using Bms.Store.Domain.Constants;
using Bms.Store.Domain.Entities;
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
            return ApiResponseDto<PagedResponseDto<AppointmentDto>>.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId ?? 0;
        var queryable = _dbContext.Appointments
            .Where(a => a.TenantId == tenantId && a.StoreId == storeId);

        if (query.CustomerId.HasValue)
            queryable = queryable.Where(a => a.CustomerId == query.CustomerId.Value);
        // 客户名称/手机号合并关键字查询：命中姓名或手机号其一即满足
        if (!string.IsNullOrWhiteSpace(query.Keyword))
            queryable = queryable.Where(a => a.CustomerName.Contains(query.Keyword) || a.CustomerPhone.Contains(query.Keyword));
        if (!string.IsNullOrWhiteSpace(query.AppointmentNo))
            queryable = queryable.Where(a => a.AppointmentNo.Contains(query.AppointmentNo));
        if (query.Status.HasValue)
            queryable = queryable.Where(a => a.Status == query.Status.Value);
        // 多状态 IN 过滤（如快速开单转单需同时匹配 1已预约/2已到店），与 Status 单值过滤叠加生效
        if (query.Statuses is { Count: > 0 })
            queryable = queryable.Where(a => query.Statuses.Contains(a.Status));
        if (query.StartTimeStart.HasValue)
            queryable = queryable.Where(a => a.StartTime.Date >= query.StartTimeStart.Value);
        if (query.StartTimeEnd.HasValue)
            queryable = queryable.Where(a => a.StartTime.Date <= query.StartTimeEnd.Value);
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

        // 查询关联技师的来源与名称，填充到 DTO
        var technicianIds = items.Where(a => a.TechnicianId.HasValue).Select(a => a.TechnicianId!.Value).Distinct().ToList();
        var technicians = await _dbContext.Technicians
            .Where(t => technicianIds.Contains(t.Id))
            .Select(t => new { t.Id, t.Source, t.Name })
            .ToListAsync();

        // 批量查询关联房间/床位名称，填充到 DTO（日历按技师展示时需显示房间名）
        var roomIds = items.Where(a => a.RoomId.HasValue).Select(a => a.RoomId!.Value).Distinct().ToList();
        var roomNames = await _dbContext.Rooms
            .Where(r => roomIds.Contains(r.Id) && !r.IsDeleted && r.TenantId == tenantId)
            .Select(r => new { r.Id, r.Name })
            .ToListAsync();

        // 批量查询关联设备名称，填充到 DTO（列表/详情展示设备名）
        var equipmentIds = items.Where(a => a.EquipmentId.HasValue).Select(a => a.EquipmentId!.Value).Distinct().ToList();
        var equipmentNames = await _dbContext.Equipments
            .Where(e => equipmentIds.Contains(e.Id) && !e.IsDeleted && e.TenantId == tenantId)
            .Select(e => new { e.Id, e.Name })
            .ToListAsync();

        // 批量查询关联商品名称，填充到 DTO（替代原 ServiceItem 字符串字段）
        var productIds = items.Select(a => a.ProductId).Distinct().ToList();
        var productNames = await _dbContext.Products
            .Where(p => productIds.Contains(p.Id))
            .Select(p => new { p.Id, Name = p.Master.Name })
            .ToListAsync();

        var dtos = items.Adapt<List<AppointmentDto>>();
        foreach (var dto in dtos)
        {
            if (dto.TechnicianId.HasValue)
            {
                var tech = technicians.FirstOrDefault(t => t.Id == dto.TechnicianId.Value);
                dto.TechnicianSource = tech?.Source;
                dto.TechnicianName = tech?.Name;
            }
            if (dto.RoomId.HasValue)
            {
                dto.RoomName = roomNames.FirstOrDefault(r => r.Id == dto.RoomId.Value)?.Name;
            }
            if (dto.EquipmentId.HasValue)
            {
                dto.EquipmentName = equipmentNames.FirstOrDefault(e => e.Id == dto.EquipmentId.Value)?.Name;
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
            return ApiResponseDto<AppointmentDto?>.Fail("登录状态异常，请重新登录", 401);

        var entity = await _dbContext.Appointments
            .FirstOrDefaultAsync(a => a.Id == id && a.TenantId == _currentUser.TenantId.Value && a.StoreId == (_currentUser.StoreId ?? 0));
        if (entity == null)
            return ApiResponseDto<AppointmentDto?>.Fail("预约不存在", 404);

        var dto = entity.Adapt<AppointmentDto>();
        if (entity.TechnicianId.HasValue)
        {
            var tech = await _dbContext.Technicians
                .FirstOrDefaultAsync(t => t.Id == entity.TechnicianId.Value);
            dto.TechnicianSource = tech?.Source;
            dto.TechnicianName = tech?.Name;
        }
        if (entity.RoomId.HasValue)
        {
            var room = await _dbContext.Rooms
                .FirstOrDefaultAsync(r => r.Id == entity.RoomId.Value && !r.IsDeleted);
            dto.RoomName = room?.Name;
        }
        if (entity.EquipmentId.HasValue)
        {
            var equipment = await _dbContext.Equipments
                .FirstOrDefaultAsync(e => e.Id == entity.EquipmentId.Value && !e.IsDeleted);
            dto.EquipmentName = equipment?.Name;
        }

        // 填充服务项目商品名称（替代原 ServiceItem 字符串字段）
        var product = await _dbContext.Products
            .Include(p => p.Master)
            .FirstOrDefaultAsync(p => p.Id == entity.ProductId);
        dto.ProductName = product?.Master?.Name;

        return ApiResponseDto<AppointmentDto?>.Ok(dto);
    }

    /// <summary>
    /// 创建预约
    /// EndTime 由后端根据 ProductId 关联的 ServiceProduct.Duration 权威计算，前端无需传入
    /// </summary>
    public async Task<ApiResponseDto<AppointmentDto>> CreateAsync(AppointmentCreateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<AppointmentDto>.Fail("登录状态异常，请重新登录", 401);

        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<AppointmentDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId ?? 0;

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
                .FirstOrDefaultAsync(r => r.Id == dto.RoomId.Value && !r.IsDeleted && r.TenantId == tenantId
                    && r.StoreId == (_currentUser.StoreId ?? 0));
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
                .FirstOrDefaultAsync(e => e.Id == dto.EquipmentId.Value && !e.IsDeleted && e.TenantId == tenantId
                    && e.StoreId == (_currentUser.StoreId ?? 0));
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

        // 计算 EndTime（基于 ServiceProduct.Duration，后端权威计算；跨日时自动进位到次日）
        var start = dto.StartTime;
        var endTime = start.AddMinutes(serviceProduct.Duration.Value);

        // 检查冲突（技师/房间/设备任一相同且时段重叠即冲突，跨 Appointment + OrderItem 双向；同一客户时段不可交集）
        var conflictMsg = await BuildConflictMessageAsync(
            tenantId, storeId, dto.CustomerId, dto.TechnicianId, dto.RoomId, dto.EquipmentId,
            start, endTime);
        if (conflictMsg != null)
            return ApiResponseDto<AppointmentDto>.Fail(conflictMsg, 400);

        var entity = dto.Adapt<AppointmentEntity>();
        entity.TenantId = tenantId;
        entity.TenantCode = _currentUser.TenantCode ?? string.Empty;
        entity.StoreId = storeId;
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

        // 事务级顾问锁：按 (租户, 预约日期) 串行化并发请求
        // AppointmentNo 采用"查max+1"生成模式，并发下需串行化避免重复
        await using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            var lockKey = AppointmentNoGenerator.BuildLockKey(tenantId, storeId, dto.StartTime);
            await _dbContext.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock({0})", lockKey);

            // 在锁保护下生成预约号（AP{yyyyMMdd}{序号}，同租户+门店+开始日递增）
            entity.AppointmentNo = await AppointmentNoGenerator.GenerateAsync(_dbContext, tenantId, storeId, dto.StartTime);

            _dbContext.Appointments.Add(entity);
            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }

        // 填充 ProductName 返回
        var resultDto = entity.Adapt<AppointmentDto>();
        resultDto.ProductName = product.Master.Name;
        return ApiResponseDto<AppointmentDto>.Ok(resultDto, "创建成功");
    }

    /// <summary>
    /// 更新预约
    /// EndTime 由后端根据 ProductId 关联的 ServiceProduct.Duration 权威计算，前端无需传入
    /// </summary>
    public async Task<ApiResponseDto<AppointmentDto>> UpdateAsync(AppointmentUpdateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<AppointmentDto>.Fail("登录状态异常，请重新登录", 401);

        var validation = await _updateValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<AppointmentDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var entity = await _dbContext.Appointments
            .FirstOrDefaultAsync(a => a.Id == dto.Id && a.TenantId == tenantId && a.StoreId == (_currentUser.StoreId ?? 0));
        if (entity == null)
            return ApiResponseDto<AppointmentDto>.Fail("预约不存在", 404);

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
                .FirstOrDefaultAsync(r => r.Id == dto.RoomId.Value && !r.IsDeleted && r.TenantId == tenantId
                    && r.StoreId == (_currentUser.StoreId ?? 0));
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
                .FirstOrDefaultAsync(e => e.Id == dto.EquipmentId.Value && !e.IsDeleted && e.TenantId == tenantId
                    && e.StoreId == (_currentUser.StoreId ?? 0));
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

        // 计算 EndTime（基于 ServiceProduct.Duration，后端权威计算；跨日时自动进位到次日）
        var start = dto.StartTime;
        var endTime = start.AddMinutes(serviceProduct.Duration.Value);

        // 检查冲突（技师/房间/设备任一相同且时段重叠即冲突，跨 Appointment + OrderItem 双向，排除自身；同一客户时段不可交集）
        var conflictMsg = await BuildConflictMessageAsync(
            tenantId, _currentUser.StoreId ?? 0, dto.CustomerId, dto.TechnicianId, dto.RoomId, dto.EquipmentId,
            start, endTime, excludeAppointmentId: dto.Id);
        if (conflictMsg != null)
            return ApiResponseDto<AppointmentDto>.Fail(conflictMsg, 400);

        // 预约号由后端生成且创建后不可变，更新时不修改
        entity.CustomerId = dto.CustomerId;
        entity.CustomerName = dto.CustomerName;
        entity.CustomerPhone = dto.CustomerPhone;
        entity.StartTime = dto.StartTime;
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
        resultDto.ProductName = product.Master.Name;
        return ApiResponseDto<AppointmentDto>.Ok(resultDto, "更新成功");
    }

    /// <summary>
    /// 检查预约冲突并返回用户可读提示信息
    /// 1. 资源冲突：委托 IResourceConflictCheckService 跨 Appointment + OrderItem 双向检测
    ///    状态过滤：Appointment.Status IN (1已预约, 2已到店) / Order.Status = 2已完成
    /// 2. 客户时段冲突：同一客户在目标时段内已有其他预约（时间段交集，仅限 Appointment 之间）
    /// </summary>
    /// <param name="customerId">客户ID（同一客户时段不可交集）</param>
    /// <param name="start">预约开始时间（由调用方从 StartTime 传入）</param>
    /// <param name="end">预约结束时间（由调用方根据 ServiceProduct.Duration 计算）</param>
    /// <returns>冲突提示信息;无冲突返回 null</returns>
    private async Task<string?> BuildConflictMessageAsync(
        long tenantId,
        long storeId,
        long customerId,
        long? technicianId,
        long? roomId,
        long? equipmentId,
        DateTime start,
        DateTime end,
        long? excludeAppointmentId = null)
    {
        var blocks = new List<string>();

        // 1. 资源冲突（技师/房间/设备）：冲突块内部"告知 + 建议"用逗号连接
        var result = await _resourceConflictCheckService.CheckAsync(
            tenantId, storeId, technicianId, roomId, equipmentId,
            start, end, excludeAppointmentId);
        var resources = new List<(string Name, string Info)>();
        if (result.TechnicianConflict)
            resources.Add(("技师", result.TechnicianConflictInfo ?? string.Empty));
        if (result.RoomConflict)
            resources.Add(("房间", result.RoomConflictInfo ?? string.Empty));
        if (result.EquipmentConflict)
            resources.Add(("设备", result.EquipmentConflictInfo ?? string.Empty));

        if (resources.Count == 1)
        {
            blocks.Add($"您选择的{resources[0].Name}在该时段已有安排（{resources[0].Info}），请更换{resources[0].Name}或调整预约时间");
        }
        else if (resources.Count > 1)
        {
            var resourceDetail = string.Join("与", resources.Select(r => $"{r.Name}（{r.Info}）"));
            blocks.Add($"您选择的{resourceDetail}在该时段已有安排，请更换{string.Join("、", resources.Select(r => r.Name))}或调整预约时间");
        }

        // 2. 同一客户时段冲突
        var customerConflict = await CheckCustomerTimeConflictAsync(
            tenantId, storeId, customerId, start, end, excludeAppointmentId);
        if (customerConflict != null)
        {
            blocks.Add($"该客户在该时段已有其他预约（{customerConflict}），请调整预约时间");
        }

        if (blocks.Count == 0)
            return null;

        // 不同冲突块之间用分号分隔，同一冲突块内部用逗号连接
        return string.Join("；", blocks);
    }

    /// <summary>
    /// 检查同一客户在目标时段内是否已有其他预约（时间段交集冲突）
    /// 规则：同一租户 + 同一门店 + 同一客户 的预约，状态 IN (1已预约, 2已到店) 且时段半开区间 [start, end) 重叠即冲突
    /// 已完成/已取消/爽约 不占用时段，不算冲突
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <param name="storeId">门店ID（按门店维度隔离）</param>
    /// <param name="customerId">客户ID</param>
    /// <param name="start">目标时段开始时间（含）</param>
    /// <param name="end">目标时段结束时间（不含）</param>
    /// <param name="excludeAppointmentId">排除的预约ID（更新场景排除自身）</param>
    /// <returns>冲突预约信息（预约号+时段）；无冲突返回 null</returns>
    private async Task<string?> CheckCustomerTimeConflictAsync(
        long tenantId,
        long storeId,
        long customerId,
        DateTime start,
        DateTime end,
        long? excludeAppointmentId = null)
    {
        var apptStatuses = new[] { AppointmentStatus.Confirmed, AppointmentStatus.Arrived };
        var query = _dbContext.Appointments
            .Where(a => a.TenantId == tenantId && a.StoreId == storeId
                && a.CustomerId == customerId
                && apptStatuses.Contains(a.Status)
                && a.EndTime.HasValue
                // 日期范围粗筛 [start.Date-1, end.Date]：兜底跨日预约（前日跨到今日 / 今日跨到明日）
                && a.StartTime.Date >= start.Date.AddDays(-1)
                && a.StartTime.Date <= end.Date);
        if (excludeAppointmentId.HasValue)
            query = query.Where(a => a.Id != excludeAppointmentId.Value);

        var appts = await query.ToListAsync();
        foreach (var a in appts)
        {
            var aStart = a.StartTime;
            var aEnd = a.EndTime!.Value;
            // 时段重叠判断：[start, end) 与 [aStart, aEnd) 重叠
            if (!(start < aEnd && aStart < end)) continue;

            return $"{a.AppointmentNo} {aStart:HH:mm}-{aEnd:HH:mm}";
        }

        return null;
    }

    /// <summary>
    /// 查询服务项目商品及其 ServiceProduct 子表
    /// 校验：商品存在 + 归属当前租户 + type=2 服务项目
    /// </summary>
    /// <returns>(Product, ServiceProduct)；若商品不存在或类型不对返回 (null, null)</returns>
    private async Task<(ProductEntity? Product, ServiceProductEntity? ServiceProduct)> GetServiceProductAsync(long productId, long tenantId)
    {
        var product = await _dbContext.Products
            .Include(p => p.Master)
            .FirstOrDefaultAsync(p => p.Id == productId && p.TenantId == tenantId && p.Master.Type == 2);
        if (product == null)
            return (null, null);

        // 注意：productId 是门店档案 Product.Id，而 ServiceProduct.MasterId 指向商品主档 ProductMaster.Id，
        // 必须用 product.MasterId 关联，否则查不到子表导致误报"服务项目未配置服务时长"
        var serviceProduct = await _dbContext.ServiceProducts
            .FirstOrDefaultAsync(sp => sp.MasterId == product.MasterId && sp.TenantId == tenantId);
        return (product, serviceProduct);
    }

    /// <summary>
    /// 删除预约
    /// </summary>
    public async Task<ApiResponseDto> DeleteAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("登录状态异常，请重新登录", 401);

        var entity = await _dbContext.Appointments
            .FirstOrDefaultAsync(a => a.Id == id && a.TenantId == _currentUser.TenantId.Value && a.StoreId == (_currentUser.StoreId ?? 0));
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
            return ApiResponseDto.Fail("登录状态异常，请重新登录", 401);
        if (ids == null || !ids.Any())
            return ApiResponseDto.Fail("请选择要删除的数据", 400);

        var entities = await _dbContext.Appointments
            .Where(a => ids.Contains(a.Id) && a.TenantId == _currentUser.TenantId.Value && a.StoreId == (_currentUser.StoreId ?? 0))
            .ToListAsync();

        _dbContext.Appointments.RemoveRange(entities);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, $"成功删除 {entities.Count} 条数据");
    }
}
