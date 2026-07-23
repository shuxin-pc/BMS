namespace Bms.Store.Application.Dtos.Inventories;

/// <summary>
/// 更新库存预警输入 DTO
/// </summary>
public class InventoryAlertUpdateDto : InventoryAlertCreateDto
{
    public long Id { get; set; }
}
