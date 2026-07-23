namespace Bms.Store.Application.Dtos.Inventories;

/// <summary>
/// 库存流水输出 DTO
/// </summary>
public class InventoryLogDto
{
    public long Id { get; set; }
    public long ProductId { get; set; }
    public int Type { get; set; }
    public int? SourceType { get; set; }
    public long? SupplierId { get; set; }
    public decimal? UnitPrice { get; set; }
    public decimal Quantity { get; set; }
    public decimal BeforeQuantity { get; set; }
    public decimal AfterQuantity { get; set; }
    public string? BatchNo { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public long? RelatedId { get; set; }
    public string? Remark { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
