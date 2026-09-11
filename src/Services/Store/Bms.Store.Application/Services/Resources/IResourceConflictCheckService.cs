using System;
using System.Threading.Tasks;
using Bms.Store.Application.Dtos.Resources;

namespace Bms.Store.Application.Services.Resources;

/// <summary>
/// 资源冲突检测服务接口
/// 技师/房间/设备 同时段占用检测（仅预约占用：Appointment.Status IN (1已预约, 2已到店)）
/// 已入库订单（Status=2 创建即完成）服务已结束、资源已释放，不参与占用
/// </summary>
public interface IResourceConflictCheckService
{
    /// <summary>
    /// 检测指定时段内技师/房间/设备是否被占用
    /// 状态过滤：Appointment.Status IN (1已预约, 2已到店)；已取消/爽约 不算占用
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <param name="storeId">门店ID（按门店维度检测占用，预约门店隔离）</param>
    /// <param name="technicianId">技师ID（null=不检测技师）</param>
    /// <param name="roomId">房间/床位ID（null=不检测房间）</param>
    /// <param name="equipmentId">设备ID（null=不检测设备）</param>
    /// <param name="startTime">占用开始时间（含）</param>
    /// <param name="endTime">占用结束时间（不含）</param>
    /// <param name="excludeAppointmentId">排除的预约ID（更新场景排除自身）</param>
    /// <returns>每个资源的冲突状态</returns>
    Task<ResourceConflictResult> CheckAsync(
        long tenantId,
        long storeId,
        long? technicianId,
        long? roomId,
        long? equipmentId,
        DateTime startTime,
        DateTime endTime,
        long? excludeAppointmentId = null);
}
