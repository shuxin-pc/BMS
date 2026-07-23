using System.Threading.Tasks;
using Bms.Store.Application.Dtos.Resources;

namespace Bms.Store.Application.Services.Resources;

/// <summary>
/// 资源可用性查询服务
/// 用于前端技师/房间/设备下拉列表的"占用标红"
/// 状态过滤：Appointment.Status IN (2已预约, 3已到店) / Order.Status = 1进行中
/// </summary>
public interface IResourceAvailabilityService
{
    /// <summary>
    /// 查询指定时段内技师/房间/设备的可用性
    /// 返回全量列表（启用状态），每项带 IsOccupied 标记供前端标红
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <param name="storeId">门店ID（可空，null=当前租户全部门店）</param>
    /// <param name="startTime">占用开始时间（含）</param>
    /// <param name="endTime">占用结束时间（不含）</param>
    /// <param name="roomType">房间类型过滤（1:房间 2:床位，null=不过滤）；serviceProductId 优先级更高</param>
    /// <param name="serviceProductId">服务项目ID（可空，传入后按其 RequiredRoomType 自动过滤房间）</param>
    /// <returns>技师/房间/设备的可用性列表</returns>
    Task<ResourceAvailabilityDto> GetAvailabilityAsync(
        long tenantId,
        long? storeId,
        System.DateTime startTime,
        System.DateTime endTime,
        int? roomType = null,
        long? serviceProductId = null);
}
