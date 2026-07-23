namespace Bms.Store.Application.Dtos.Inventories;

/// <summary>
/// 创建库存输入 DTO
/// </summary>
public class InventoryCreateDto
{
    public long ProductId { get; set; }
    public decimal Quantity { get; set; }
    public decimal AlertQuantity { get; set; }
}
