namespace Bms.Store.Application.Dtos.Inventories;

/// <summary>
/// 库存预警输出 DTO
/// </summary>
public class InventoryAlertDto
{
    public long Id { get; set; }
    public long ProductId { get; set; }
    public int AlertType { get; set; }
    public decimal CurrentQuantity { get; set; }
    public decimal AlertValue { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public bool IsProcessed { get; set; }
    public DateTime? ProcessedTime { get; set; }
    public string? ProcessedRemark { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
