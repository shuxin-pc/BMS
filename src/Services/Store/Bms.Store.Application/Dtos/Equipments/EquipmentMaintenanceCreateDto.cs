namespace Bms.Store.Application.Dtos.Equipments;

/// <summary>
/// 创建设备维护记录DTO
/// </summary>
public class EquipmentMaintenanceCreateDto
{
    public long EquipmentId { get; set; }
    public int MaintenanceType { get; set; }
    public DateTime MaintenanceDate { get; set; }
    public decimal? Cost { get; set; }
    public string? Operator { get; set; }
    public string? Result { get; set; }
    public DateTime? NextMaintenanceDate { get; set; }
    public string? Remark { get; set; }
}
