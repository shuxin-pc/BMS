namespace Bms.Store.Application.Dtos.Equipments;

/// <summary>
/// 更新设备台账输入 DTO
/// </summary>
public class EquipmentUpdateDto : EquipmentCreateDto
{
    public long Id { get; set; }
}
