namespace Bms.Store.Application.Dtos.StockTransfers;

/// <summary>
/// 创建库存调拨单明细输入 DTO
/// </summary>
public class StockTransferItemCreateDto
{
    public long StockTransferId { get; set; }
    public long ProductId { get; set; }
    public decimal Quantity { get; set; }
    public string? BatchNo { get; set; }
    public string? Remark { get; set; }
}
