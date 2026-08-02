namespace Bms.Store.Domain.Entities;

/// <summary>
/// 样品赠品调拨明细
/// 记录调拨单中每个商品的调拨数量与批次信息
/// 软删除：无，随主表级联删除
/// </summary>
public class SampleGiftTransferItem : StoreBusinessEntityBase
{
    /// <summary>调拨单ID</summary>
    public long SampleGiftTransferId { get; set; }

    /// <summary>商品ID（Type∈{4,5}：样品或赠品）</summary>
    public long ProductId { get; set; }

    /// <summary>商品名称（冗余存储，便于展示）</summary>
    public string? ProductName { get; set; }

    /// <summary>商品编码（冗余存储，便于展示）</summary>
    public string? ProductCode { get; set; }

    /// <summary>商品单位（冗余存储，便于展示）</summary>
    public string? Unit { get; set; }

    /// <summary>调拨数量</summary>
    public decimal Quantity { get; set; }

    /// <summary>手动模式指定批次号；null=FEFO自动分配</summary>
    public string? BatchNo { get; set; }

    /// <summary>备注</summary>
    public string? Remark { get; set; }

    /// <summary>导航属性：调拨单</summary>
    public SampleGiftTransfer? SampleGiftTransfer { get; set; }

    /// <summary>导航属性：商品</summary>
    public Product? Product { get; set; }
}
