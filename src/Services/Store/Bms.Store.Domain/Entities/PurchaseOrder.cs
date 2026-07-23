namespace Bms.Store.Domain.Entities;

/// <summary>
/// 采购入库单
/// 店主向供应商采购商品，入库后自动增加库存
/// </summary>
public class PurchaseOrder : StoreBusinessEntityBase
{
    /// <summary>
    /// 采购单号
    /// </summary>
    public string OrderNo { get; set; } = string.Empty;

    /// <summary>
    /// 供应商ID
    /// </summary>
    public long SupplierId { get; set; }

    /// <summary>
    /// 采购日期
    /// </summary>
    public DateTime OrderDate { get; set; }

    /// <summary>
    /// 采购总金额
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// 已退货总金额（采购退货冲减，初始为 0）
    /// 用于报表对账：实际采购成本 = TotalAmount - RefundedAmount
    /// </summary>
    public decimal RefundedAmount { get; set; }

    /// <summary>
    /// 状态（1:待审核 2:已审核 3:已入库 4:已取消）
    /// </summary>
    public int Status { get; set; } = 1;

    /// <summary>
    /// 采购类型（1:零售商品采购 2:耗材采购）
    /// </summary>
    public int PurchaseType { get; set; }

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
    /// 导航属性：采购单明细列表（级联创建，入库时联动库存）
    /// </summary>
    public List<PurchaseOrderItem> OrderItems { get; set; } = new();
}
