namespace Bms.Store.Application.Dtos.Equipments;

/// <summary>
/// 创建设备台账输入 DTO
/// </summary>
public class EquipmentCreateDto
{
    /// <summary>
    /// 所属设备类型 ID（必填，需为当前租户下已存在的类型）
    /// </summary>
    public long EquipmentTypeId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Model { get; set; }
    public string? Manufacturer { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public decimal? PurchasePrice { get; set; }
    public int Status { get; set; }
    public string? Location { get; set; }
    public DateTime? LastMaintenanceDate { get; set; }
    public DateTime? NextMaintenanceDate { get; set; }
    /// <summary>
    /// 保养周期（天），null=不定期/手动指定
    /// </summary>
    public int? MaintenanceCycleDays { get; set; }
    public string? Remark { get; set; }
}
