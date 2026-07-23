namespace Bms.Store.Application.Dtos.Inventories;

/// <summary>
/// 创建库存流水输入 DTO
/// </summary>
public class InventoryLogCreateDto
{
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
}
