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
    /// 商品名称（冗余存储，便于展示）
    /// </summary>
    public string? ProductName { get; set; }

    /// <summary>
    /// 商品编码（冗余存储，便于展示）
    /// </summary>
    public string? ProductCode { get; set; }

    /// <summary>
    /// 商品单位（冗余存储，便于展示）
    /// </summary>
    public string? Unit { get; set; }

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
