namespace Bms.Store.Domain.Entities;

/// <summary>
/// 设备保养记录
/// 记录设备保养/维修情况，支撑保养提醒功能
/// </summary>
public class EquipmentMaintenance : StoreBusinessEntityBase
{
    /// <summary>
    /// 设备ID
    /// </summary>
    public long EquipmentId { get; set; }

    /// <summary>
    /// 保养类型（1:日常保养 2:定期保养 3:维修）
    /// </summary>
    public int MaintenanceType { get; set; }

    /// <summary>
    /// 保养日期
    /// </summary>
    public DateTime MaintenanceDate { get; set; }

    /// <summary>
    /// 保养费用
    /// </summary>
    public decimal? Cost { get; set; }

    /// <summary>
    /// 操作人
    /// </summary>
    public string? Operator { get; set; }

    /// <summary>
    /// 保养结果
    /// </summary>
    public string? Result { get; set; }

    /// <summary>
    /// 下次保养日期（覆盖设备上的下次保养日期）
    /// </summary>
    public DateTime? NextMaintenanceDate { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 导航属性：设备
    /// </summary>
    public Equipment? Equipment { get; set; }
}
