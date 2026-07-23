using Bms.Store.Application.Dtos;

namespace Bms.Store.Application.Dtos.Equipments;

/// <summary>
/// 设备维护记录分页查询DTO
/// </summary>
public class EquipmentMaintenanceQueryDto : PagedRequestDto
{
    public long? EquipmentId { get; set; }
    public int? MaintenanceType { get; set; }

    /// <summary>
    /// 保养日期范围-开始（含当天）
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// 保养日期范围-结束（含当天）
    /// </summary>
    public DateTime? EndDate { get; set; }
}
