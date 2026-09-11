using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Bms.Store.Application.Dtos.Resources;
using Bms.Store.Domain.Constants;
using Bms.Store.Domain.Entities;
using Bms.Store.Infrastructure;

namespace Bms.Store.Application.Services.Resources;

/// <summary>
/// 资源可用性查询服务实现
/// 返回技师/房间/设备的全量列表，每项带 IsOccupied 标记
/// 仅预约占用（Appointment.Status IN 1已预约, 2已到店）；已入库订单（Status=2 创建即完成）服务已结束、资源已释放，不参与占用
/// 占用判定复用 IResourceConflictCheckService 的查询逻辑（按天过滤 + 半开区间 [start, end) 重叠算法）
/// </summary>
public class ResourceAvailabilityService : IResourceAvailabilityService
{
    /// <summary>
    /// 平台租户ID（技师支持跨租户选择）
    /// </summary>
    private const long PlatformTenantId = 1;

    private readonly StoreDbContext _dbContext;

    public ResourceAvailabilityService(StoreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ResourceAvailabilityDto> GetAvailabilityAsync(
        long tenantId,
        long? storeId,
        global::System.DateTime startTime,
        global::System.DateTime endTime,
        int? roomType = null,
        long? serviceProductId = null,
        long? excludeAppointmentId = null)
    {
        var result = new ResourceAvailabilityDto();

        // 计算生效的房间类型过滤：serviceProductId 优先（取其 RequiredRoomType），其次 roomType
        // 服务项目未配置 RequiredRoomType 时回退使用 roomType
        int? effectiveRoomType = roomType;
        if (serviceProductId.HasValue)
        {
            var sp = await _dbContext.ServiceProducts
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.MasterId == serviceProductId.Value);
            if (sp?.RequiredRoomType.HasValue == true)
            {
                effectiveRoomType = sp.RequiredRoomType;
            }
        }

        // 加载技师列表（含平台技师）+ 房间列表 + 设备列表
        var technicians = await _dbContext.Technicians
            .Where(t => !t.IsDeleted && t.Status == 1
                && (t.TenantId == tenantId || t.TenantId == PlatformTenantId))
            .Select(t => new { t.Id, t.Name, t.Source })
            .ToListAsync();

        var rooms = await _dbContext.Rooms
            .Where(r => !r.IsDeleted && r.Status == 1 && r.TenantId == tenantId
                && (!storeId.HasValue || r.StoreId == storeId.Value))
            .Where(r => effectiveRoomType == null || r.RoomType == effectiveRoomType)
            .Select(r => new { r.Id, r.Name, r.RoomType, r.Status })
            .ToListAsync();

        var equipments = await _dbContext.Equipments
            .Where(e => !e.IsDeleted && e.Status == EquipmentStatus.Normal && e.TenantId == tenantId
                && (!storeId.HasValue || e.StoreId == storeId.Value))
            .Select(e => new { e.Id, e.Name, e.Status })
            .ToListAsync();

        // 查询日期范围内的所有占用记录（按租户 + 日期范围粗筛）
        // 范围 [startTime.Date-1, endTime.Date]：兜底跨日预约占用（前日跨到今日 / 今日跨到明日）
        var startDate = startTime.Date.AddDays(-1);
        var endDate = endTime.Date;
        var apptStatuses = new[] { AppointmentStatus.Confirmed, AppointmentStatus.Arrived };

        var appointments = await _dbContext.Appointments
            .Where(a => a.TenantId == tenantId
                && (!storeId.HasValue || a.StoreId == storeId.Value)
                && apptStatuses.Contains(a.Status)
                && a.StartTime.Date >= startDate
                && a.StartTime.Date <= endDate)
            .ToListAsync();

        // 构建占用时间区间列表：[(resourceType, resourceId, start, end, source, info)]
        // 仅预约占用（Appointment.Status IN 1已预约, 2已到店）；已入库订单服务已结束、资源已释放，不参与占用
        var technicianOccupancy = new List<(long Id, DateTime Start, DateTime End, string Source, string Info)>();
        var roomOccupancy = new List<(long Id, DateTime Start, DateTime End, string Source, string Info)>();
        var equipmentOccupancy = new List<(long Id, DateTime Start, DateTime End, string Source, string Info)>();

        foreach (var a in appointments)
        {
            // 预约转订单行编辑时排除其源预约占用：该占用由本次转单继承（后端 OrderAppService 对源预约订单跳过冲突校验），
            // 若计入则编辑弹窗会"自己标红自己"且保存被拦截
            if (excludeAppointmentId.HasValue && a.Id == excludeAppointmentId.Value) continue;

            var aStart = a.StartTime;
            // EndTime 由 AppointmentAppService 创建/更新时根据 ServiceProduct.Duration 自动计算
            DateTime? aEnd = a.EndTime;
            if (!aEnd.HasValue) continue;
            var aEndVal = aEnd.Value;

            var info = $"预约单 {a.AppointmentNo} {aStart:HH:mm}-{aEndVal:HH:mm}";

            if (a.TechnicianId.HasValue)
                technicianOccupancy.Add((a.TechnicianId.Value, aStart, aEndVal, "appointment", info));
            if (a.RoomId.HasValue)
                roomOccupancy.Add((a.RoomId.Value, aStart, aEndVal, "appointment", info));
            if (a.EquipmentId.HasValue)
                equipmentOccupancy.Add((a.EquipmentId.Value, aStart, aEndVal, "appointment", info));
        }

        // 对每个资源，判断在 [startTime, endTime) 内是否被任意占用区间覆盖
        result.Technicians = technicians.Select(t =>
        {
            var (occupied, source, info) = FindOverlap(t.Id, technicianOccupancy, startTime, endTime);
            return new TechnicianAvailabilityItem
            {
                Id = t.Id,
                Name = t.Name,
                Source = t.Source,
                IsOccupied = occupied,
                ConflictSource = occupied ? source : null,
                ConflictInfo = occupied ? info : null
            };
        }).ToList();

        result.Rooms = rooms.Select(r =>
        {
            var (occupied, source, info) = FindOverlap(r.Id, roomOccupancy, startTime, endTime);
            return new RoomAvailabilityItem
            {
                Id = r.Id,
                Name = r.Name,
                RoomType = r.RoomType,
                Status = r.Status,
                IsOccupied = occupied,
                ConflictSource = occupied ? source : null,
                ConflictInfo = occupied ? info : null
            };
        }).ToList();

        result.Equipments = equipments.Select(e =>
        {
            var (occupied, source, info) = FindOverlap(e.Id, equipmentOccupancy, startTime, endTime);
            return new EquipmentAvailabilityItem
            {
                Id = e.Id,
                Name = e.Name,
                Status = e.Status,
                IsOccupied = occupied,
                ConflictSource = occupied ? source : null,
                ConflictInfo = occupied ? info : null
            };
        }).ToList();

        return result;
    }

    /// <summary>
    /// 判断指定资源在 [start, end) 时段内是否被任意占用区间覆盖
    /// 重叠算法：startTime < occEnd && occStart < endTime
    /// </summary>
    private static (bool Occupied, string? Source, string? Info) FindOverlap(
        long resourceId,
        List<(long Id, DateTime Start, DateTime End, string Source, string Info)> occupancy,
        DateTime startTime,
        DateTime endTime)
    {
        foreach (var occ in occupancy)
        {
            if (occ.Id != resourceId) continue;
            if (startTime < occ.End && occ.Start < endTime)
            {
                return (true, occ.Source, occ.Info);
            }
        }
        return (false, null, null);
    }
}
