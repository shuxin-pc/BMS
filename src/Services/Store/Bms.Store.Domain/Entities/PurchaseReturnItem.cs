namespace Bms.Store.Domain.Entities;

/// <summary>
/// 采购退货明细
/// 一张退货单可包含多个商品明细，明细继承 StoreBusinessEntityBase（无软删除）
/// </summary>
public class PurchaseReturnItem : StoreBusinessEntityBase
{
    /// <summary>
    /// 退货单ID
    /// </summary>
    public long PurchaseReturnId { get; set; }

    /// <summary>
    /// 商品ID
    /// </summary>
    public long ProductId { get; set; }

    /// <summary>
    /// 退货数量
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// 退款金额
    /// </summary>
    public decimal RefundAmount { get; set; }

    /// <summary>
    /// 批次号（可选，用于指定退回哪个批次）
    /// </summary>
    public string? BatchNo { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 导航属性：退货单
    /// </summary>
    public PurchaseReturn? PurchaseReturn { get; set; }

    /// <summary>
    /// 导航属性：商品
    /// </summary>
    public Product? Product { get; set; }
}
