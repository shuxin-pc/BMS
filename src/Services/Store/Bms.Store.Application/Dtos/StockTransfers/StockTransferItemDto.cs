namespace Bms.Store.Application.Dtos.StockTransfers;

/// <summary>
/// 库存调拨单明细输出 DTO
/// </summary>
public class StockTransferItemDto
{
    public long Id { get; set; }
    public long StockTransferId { get; set; }
    public long ProductId { get; set; }
    public decimal Quantity { get; set; }
    public string? BatchNo { get; set; }
    public string? Remark { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
