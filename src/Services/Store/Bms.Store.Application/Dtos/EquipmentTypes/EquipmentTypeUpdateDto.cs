namespace Bms.Store.Application.Dtos.EquipmentTypes;

/// <summary>
/// 更新设备类型输入 DTO
/// </summary>
public class EquipmentTypeUpdateDto : EquipmentTypeCreateDto
{
    public long Id { get; set; }
}
