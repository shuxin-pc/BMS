namespace Bms.Store.Application.Dtos.Inventories;

/// <summary>
/// 更新库存输入 DTO
/// </summary>
public class InventoryUpdateDto : InventoryCreateDto
{
    public long Id { get; set; }
}
