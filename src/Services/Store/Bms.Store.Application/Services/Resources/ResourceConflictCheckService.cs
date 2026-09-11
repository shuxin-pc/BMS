using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Bms.Store.Application.Dtos.Resources;
using Bms.Store.Domain.Entities;
using Bms.Store.Infrastructure;

namespace Bms.Store.Application.Services.Resources;

/// <summary>
/// 资源冲突检测服务实现
/// 仅预约占用（Appointment.Status IN 1已预约, 2已到店），按半开区间 [start, end) 重叠算法
/// 已入库订单（Status=2 创建即完成）服务已结束、资源已释放，不参与占用
/// </summary>
public class ResourceConflictCheckService : IResourceConflictCheckService
{
    private readonly StoreDbContext _dbContext;

    public ResourceConflictCheckService(StoreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ResourceConflictResult> CheckAsync(
        long tenantId,
        long storeId,
        long? technicianId,
        long? roomId,
        long? equipmentId,
        DateTime startTime,
        DateTime endTime,
        long? excludeAppointmentId = null)
    {
        var result = new ResourceConflictResult();

        // 1. Appointment 表查询：状态 1已预约 / 2已到店（按门店维度隔离）
        // 日期范围粗筛 [startTime.Date-1, endTime.Date]：兜底跨日预约（前日跨到今日 / 今日跨到明日）
        var apptStatuses = new[] { AppointmentStatus.Confirmed, AppointmentStatus.Arrived };
        var apptQuery = _dbContext.Appointments
            .Where(a => a.TenantId == tenantId && a.StoreId == storeId
                && apptStatuses.Contains(a.Status)
                && a.StartTime.Date >= startTime.Date.AddDays(-1)
                && a.StartTime.Date <= endTime.Date);
        if (excludeAppointmentId.HasValue)
            apptQuery = apptQuery.Where(a => a.Id != excludeAppointmentId.Value);

        var appts = await apptQuery.ToListAsync();
        foreach (var a in appts)
        {
            var aStart = a.StartTime;
            // EndTime 由 AppointmentAppService 创建/更新时根据 ServiceProduct.Duration 自动计算
            DateTime? aEnd = a.EndTime;
            if (!aEnd.HasValue) continue;

            // 时段重叠判断：[startTime, endTime) 与 [aStart, aEnd) 重叠
            if (!(startTime < aEnd.Value && aStart < endTime)) continue;

            if (!result.TechnicianConflict && technicianId.HasValue && a.TechnicianId == technicianId)
            {
                result.TechnicianConflict = true;
                result.TechnicianConflictSource = "appointment";
                result.TechnicianConflictInfo = $"预约单 {a.AppointmentNo} {aStart:HH:mm}-{aEnd:HH:mm}";
            }
            if (!result.RoomConflict && roomId.HasValue && a.RoomId == roomId)
            {
                result.RoomConflict = true;
                result.RoomConflictSource = "appointment";
                result.RoomConflictInfo = $"预约单 {a.AppointmentNo} {aStart:HH:mm}-{aEnd:HH:mm}";
            }
            if (!result.EquipmentConflict && equipmentId.HasValue && a.EquipmentId == equipmentId)
            {
                result.EquipmentConflict = true;
                result.EquipmentConflictSource = "appointment";
                result.EquipmentConflictInfo = $"预约单 {a.AppointmentNo} {aStart:HH:mm}-{aEnd:HH:mm}";
            }
        }

        return result;
    }
}
