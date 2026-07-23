namespace Bms.Store.Application.Dtos.Inventories;

/// <summary>
/// 库存输出 DTO
/// </summary>
public class InventoryDto
{
    public long Id { get; set; }
    public long ProductId { get; set; }
    public decimal Quantity { get; set; }
    public decimal AlertQuantity { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
