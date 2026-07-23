namespace Bms.Store.Application.Dtos.InventoryBatches;

/// <summary>
/// 库存批次输出 DTO
/// </summary>
public class InventoryBatchDto
{
    public long Id { get; set; }
    public long ProductId { get; set; }
    public string BatchNo { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    /// <summary>
    /// 生产日期
    /// </summary>
    public DateTime? ProductionDate { get; set; }

    /// <summary>
    /// 保质期天数
    /// </summary>
    public int? ShelfLifeDays { get; set; }

    /// <summary>
    /// 过期日期
    /// </summary>
    public DateTime? ExpirationDate { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public int Status { get; set; }
    public string? Remark { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
