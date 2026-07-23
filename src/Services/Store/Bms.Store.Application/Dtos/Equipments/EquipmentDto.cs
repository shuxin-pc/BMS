using Bms.Store.Domain.Constants;

namespace Bms.Store.Application.Dtos.Equipments;

/// <summary>
/// 设备台账输出 DTO
/// </summary>
public class EquipmentDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Model { get; set; }
    public string? Manufacturer { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public decimal? PurchasePrice { get; set; }

    /// <summary>
    /// 状态：1=正常 2=维修中 3=已停用（参见 <see cref="EquipmentStatus"/>）
    /// </summary>
    public int Status { get; set; }

    /// <summary>
    /// 状态中文名称（基于 <see cref="EquipmentStatus.GetName"/>）
    /// </summary>
    public string StatusName => EquipmentStatus.GetName(Status);

    public string? Location { get; set; }
    public DateTime? LastMaintenanceDate { get; set; }
    public DateTime? NextMaintenanceDate { get; set; }
    /// <summary>
    /// 保养周期（天），null=不定期
    /// </summary>
    public int? MaintenanceCycleDays { get; set; }
    public string? Remark { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
