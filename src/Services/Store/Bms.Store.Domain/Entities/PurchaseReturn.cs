namespace Bms.Store.Domain.Entities;

/// <summary>
/// 采购退货实体
/// 店主向供应商退回商品，系统自动扣减库存并冲减采购成本
/// 支持一次退回多种商品，明细存储在 PurchaseReturnItem 中
/// </summary>
public class PurchaseReturn : StoreBusinessEntityBase
{
    /// <summary>
    /// 退货单号
    /// </summary>
    public string ReturnNo { get; set; } = string.Empty;

    /// <summary>
    /// 供应商ID
    /// </summary>
    public long SupplierId { get; set; }

    /// <summary>
    /// 关联的原采购订单ID（可选，用于冲减采购订单 RefundedAmount）
    /// null=未关联具体采购单（如历史数据或独立退货）
    /// </summary>
    public long? PurchaseOrderId { get; set; }

    /// <summary>
    /// 退货总数量（明细自动汇总）
    /// </summary>
    public decimal TotalQuantity { get; set; }

    /// <summary>
    /// 退款总金额（明细自动汇总）
    /// </summary>
    public decimal TotalRefundAmount { get; set; }

    /// <summary>
    /// 退货时间
    /// </summary>
    public DateTime ReturnTime { get; set; }

    /// <summary>
    /// 凭证照片URL（非必填）
    /// </summary>
    public string? VoucherImageUrl { get; set; }

    /// <summary>
    /// 操作员ID
    /// </summary>
    public long? OperatorId { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 导航属性：供应商
    /// </summary>
    public Supplier? Supplier { get; set; }

    /// <summary>
    /// 导航属性：关联的原采购订单（可选）
    /// </summary>
    public PurchaseOrder? PurchaseOrder { get; set; }

    /// <summary>
    /// 导航属性：退货明细列表
    /// </summary>
    public List<PurchaseReturnItem> Items { get; set; } = new();
}
