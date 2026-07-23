namespace Bms.Store.Domain.Entities;

/// <summary>
/// 采购入库单明细
/// </summary>
public class PurchaseOrderItem : StoreBusinessEntityBase
{
    /// <summary>
    /// 采购单ID
    /// </summary>
    public long PurchaseOrderId { get; set; }

    /// <summary>
    /// 商品ID
    /// </summary>
    public long ProductId { get; set; }

    /// <summary>
    /// 采购数量
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// 采购单价
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// 小计金额
    /// </summary>
    public decimal TotalPrice { get; set; }

    /// <summary>
    /// 批次号
    /// </summary>
    public string? BatchNo { get; set; }

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
    /// 入库时录入"生产日期+保质期天数"或"过期日期"二者之一：
    /// 若录入前两项，系统自动计算 ExpirationDate = ProductionDate + ShelfLifeDays 天
    /// </summary>
    public DateTime? ExpirationDate { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 导航属性：采购单
    /// </summary>
    public PurchaseOrder? PurchaseOrder { get; set; }

    /// <summary>
    /// 导航属性：商品
    /// </summary>
    public Product? Product { get; set; }
}
