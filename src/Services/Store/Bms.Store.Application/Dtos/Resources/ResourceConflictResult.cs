namespace Bms.Store.Application.Dtos.Resources;

/// <summary>
/// 资源冲突检测结果
/// 用于预约/订单创建时返回每个资源的冲突状态
/// </summary>
public class ResourceConflictResult
{
    /// <summary>
    /// 技师是否冲突
    /// </summary>
    public bool TechnicianConflict { get; set; }

    /// <summary>
    /// 技师冲突来源："appointment" / "order"
    /// </summary>
    public string? TechnicianConflictSource { get; set; }

    /// <summary>
    /// 技师冲突描述（占用单号 + 时段）
    /// </summary>
    public string? TechnicianConflictInfo { get; set; }

    /// <summary>
    /// 房间是否冲突
    /// </summary>
    public bool RoomConflict { get; set; }

    public string? RoomConflictSource { get; set; }
    public string? RoomConflictInfo { get; set; }

    /// <summary>
    /// 设备是否冲突
    /// </summary>
    public bool EquipmentConflict { get; set; }

    public string? EquipmentConflictSource { get; set; }
    public string? EquipmentConflictInfo { get; set; }

    /// <summary>
    /// 是否有任意资源冲突
    /// </summary>
    public bool HasAnyConflict => TechnicianConflict || RoomConflict || EquipmentConflict;
}
