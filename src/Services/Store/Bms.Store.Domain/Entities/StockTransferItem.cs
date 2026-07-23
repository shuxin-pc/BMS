namespace Bms.Store.Domain.Entities;

/// <summary>
/// 库存调拨单明细
/// </summary>
public class StockTransferItem : StoreBusinessEntityBase
{
    /// <summary>
    /// 调拨单ID
    /// </summary>
    public long StockTransferId { get; set; }

    /// <summary>
    /// 商品ID
    /// </summary>
    public long ProductId { get; set; }

    /// <summary>
    /// 调拨数量
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// 批次号
    /// </summary>
    public string? BatchNo { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 导航属性：调拨单
    /// </summary>
    public StockTransfer? StockTransfer { get; set; }

    /// <summary>
    /// 导航属性：商品
    /// </summary>
    public Product? Product { get; set; }
}
