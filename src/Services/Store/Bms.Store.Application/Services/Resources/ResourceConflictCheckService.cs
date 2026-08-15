using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Bms.Store.Application.Dtos.Resources;
using Bms.Store.Infrastructure;

namespace Bms.Store.Application.Services.Resources;

/// <summary>
/// 资源冲突检测服务实现
/// 跨 Appointment + OrderItem 双向互查，按半开区间 [start, end) 重叠算法
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
        long? technicianId,
        long? roomId,
        long? equipmentId,
        DateTime startTime,
        DateTime endTime,
        long? excludeAppointmentId = null,
        long? excludeOrderId = null)
    {
        var result = new ResourceConflictResult();

        // 1. Appointment 表查询：状态 2已预约 / 3已到店
        // 日期范围粗筛 [startTime.Date-1, endTime.Date]：兜底跨日预约（前日跨到今日 / 今日跨到明日）
        var apptStatuses = new[] { 2, 3 };
        var apptQuery = _dbContext.Appointments
            .Where(a => a.TenantId == tenantId
                && apptStatuses.Contains(a.Status)
                && a.AppointmentDate.Date >= startTime.Date.AddDays(-1)
                && a.AppointmentDate.Date <= endTime.Date);
        if (excludeAppointmentId.HasValue)
            apptQuery = apptQuery.Where(a => a.Id != excludeAppointmentId.Value);

        var appts = await apptQuery.ToListAsync();
        foreach (var a in appts)
        {
            var aStart = a.AppointmentDate.Date.Add(a.AppointmentTime);
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

        // 2. OrderItem 表查询：Order.Status = 1进行中，时段 = Order.OrderTime + ServiceProduct.Duration
        // 任一资源需要检测时才查询（性能优化）
        if (technicianId.HasValue || roomId.HasValue || equipmentId.HasValue)
        {
            var orderItemQuery = _dbContext.OrderItems
                .Include(oi => oi.Order)
                .Where(oi => oi.Order!.TenantId == tenantId
                    && oi.Order.Status == 1
                    && oi.Order.OrderTime.Date >= startTime.Date.AddDays(-1)
                    && oi.Order.OrderTime.Date <= endTime.Date);
            if (excludeOrderId.HasValue)
                orderItemQuery = orderItemQuery.Where(oi => oi.OrderId != excludeOrderId.Value);

            var orderItems = await orderItemQuery.ToListAsync();
            if (orderItems.Any())
            {
                // 预加载 ServiceProduct.Duration（按 MasterId 关联）
                // ServiceProduct 关联字段从 ProductId 改为 MasterId（设计文档 3.4 节）
                // 查询链路：OrderItem.ProductId -> Product.MasterId -> ServiceProduct.MasterId
                var productIds = orderItems.Select(oi => oi.ProductId).Distinct().ToList();
                var productMasterMap = await _dbContext.Products
                    .Where(p => productIds.Contains(p.Id))
                    .Select(p => new { p.Id, p.MasterId })
                    .ToDictionaryAsync(p => p.Id, p => p.MasterId);
                var masterIds = productMasterMap.Values.Distinct().ToList();
                var durationsByMaster = await _dbContext.ServiceProducts
                    .Where(sp => masterIds.Contains(sp.MasterId))
                    .ToDictionaryAsync(sp => sp.MasterId, sp => sp.Duration ?? 0);

                foreach (var oi in orderItems)
                {
                    if (!productMasterMap.TryGetValue(oi.ProductId, out var masterId)) continue;
                    if (!durationsByMaster.TryGetValue(masterId, out var duration) || duration <= 0) continue;
                    var oStart = oi.Order!.OrderTime;
                    var oEnd = oStart.AddMinutes(duration);

                    if (!(startTime < oEnd && oStart < endTime)) continue;

                    if (!result.TechnicianConflict && technicianId.HasValue && oi.TechnicianId == technicianId)
                    {
                        result.TechnicianConflict = true;
                        result.TechnicianConflictSource = "order";
                        result.TechnicianConflictInfo = $"订单 {oi.Order.OrderNo} {oStart:HH:mm}-{oEnd:HH:mm}";
                    }
                    if (!result.RoomConflict && roomId.HasValue && oi.RoomId == roomId)
                    {
                        result.RoomConflict = true;
                        result.RoomConflictSource = "order";
                        result.RoomConflictInfo = $"订单 {oi.Order.OrderNo} {oStart:HH:mm}-{oEnd:HH:mm}";
                    }
                    if (!result.EquipmentConflict && equipmentId.HasValue && oi.EquipmentId == equipmentId)
                    {
                        result.EquipmentConflict = true;
                        result.EquipmentConflictSource = "order";
                        result.EquipmentConflictInfo = $"订单 {oi.Order.OrderNo} {oStart:HH:mm}-{oEnd:HH:mm}";
                    }
                }
            }
        }

        return result;
    }
}
