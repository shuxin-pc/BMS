namespace Bms.Store.Application.Dtos.Inventories;

/// <summary>
/// 更新库存盘点记录输入 DTO
/// </summary>
public class InventoryCheckUpdateDto : InventoryCheckCreateDto
{
    public long Id { get; set; }
}
