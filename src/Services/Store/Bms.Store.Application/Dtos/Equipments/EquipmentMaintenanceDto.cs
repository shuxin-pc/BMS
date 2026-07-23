namespace Bms.Store.Application.Dtos.Equipments;

/// <summary>
/// 设备维护记录DTO
/// </summary>
public class EquipmentMaintenanceDto
{
    public long Id { get; set; }
    public long EquipmentId { get; set; }

    /// <summary>
    /// 关联设备名称（展示用，由服务层关联查询填充）
    /// </summary>
    public string? EquipmentName { get; set; }

    public int MaintenanceType { get; set; }
    public DateTime MaintenanceDate { get; set; }
    public decimal? Cost { get; set; }
    public string? Operator { get; set; }
    public string? Result { get; set; }
    public DateTime? NextMaintenanceDate { get; set; }
    public string? Remark { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
