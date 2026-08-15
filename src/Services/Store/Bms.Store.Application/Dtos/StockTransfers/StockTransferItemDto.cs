namespace Bms.Store.Application.Dtos.StockTransfers;

/// <summary>
/// 库存调拨单明细输出 DTO
/// </summary>
public class StockTransferItemDto
{
    public long Id { get; set; }
    public long StockTransferId { get; set; }
    public long ProductId { get; set; }
    public string? ProductName { get; set; }
    public string? ProductCode { get; set; }

    /// <summary>
    /// 商品类型（1:实物商品 2:服务商品 3:耗材 4:样品 5:赠品）
    /// 由 Mapster 从 Product.Master.Type 映射
    /// </summary>
    public int? Type { get; set; }

    public string? Unit { get; set; }
    public decimal Quantity { get; set; }
    public string? BatchNo { get; set; }
    public string? Remark { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
