namespace Bms.Store.Application.Dtos.Inventories;

/// <summary>
/// 创建库存预警输入 DTO
/// </summary>
public class InventoryAlertCreateDto
{
    public long ProductId { get; set; }
    public int AlertType { get; set; }
    public decimal CurrentQuantity { get; set; }
    public decimal AlertValue { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public bool IsProcessed { get; set; }
    public DateTime? ProcessedTime { get; set; }
    public string? ProcessedRemark { get; set; }
}
