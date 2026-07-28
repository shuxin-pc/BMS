namespace Bms.Store.Domain.Entities;

/// <summary>
/// 库存流水
/// </summary>
public class InventoryLog : StoreBusinessEntityBase
{
    /// <summary>
    /// 商品ID
    /// </summary>
    public long ProductId { get; set; }

    /// <summary>
    /// 库存操作类型（1:入库 2:出库）
    /// </summary>
    public int Type { get; set; }

    /// <summary>
    /// 来源类型（标识业务来源，入库与出库均可设置）
    /// 取值见 <see cref="InventoryLogSourceTypes"/>：0=销售出库 1=采购入库 2=退货入库 3=盘点调整 4=调拨入库 5=调拨出库 6=其他 7=采购退货出库 8=疗程卡核销出库 9=样品赠品出库(历史合并值) 10=样品领用出库 11=赠品活动出库
    /// </summary>
    public int? SourceType { get; set; }

    /// <summary>
    /// 供应商ID（采购入库时关联）
    /// </summary>
    public long? SupplierId { get; set; }

    /// <summary>
    /// 单价（用于FIFO成本计算）
    /// </summary>
    public decimal? UnitPrice { get; set; }

    /// <summary>
    /// 数量变化（正数增加，负数减少）
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// 操作前库存
    /// </summary>
    public decimal BeforeQuantity { get; set; }

    /// <summary>
    /// 操作后库存
    /// </summary>
    public decimal AfterQuantity { get; set; }

    /// <summary>
    /// 批次号
    /// </summary>
    public string? BatchNo { get; set; }

    /// <summary>
    /// 过期日期
    /// </summary>
    public DateTime? ExpirationDate { get; set; }

    /// <summary>
    /// 关联单据ID（如采购单ID、订单ID）
    /// </summary>
    public long? RelatedId { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 操作人姓名（冗余存储，写入时取 ICurrentUser.RealName ?? UserName）
    /// </summary>
    public string? OperatorName { get; set; }

    /// <summary>
    /// 导航属性：商品
    /// </summary>
    public Product? Product { get; set; }

    /// <summary>
    /// 导航属性：供应商
    /// </summary>
    public Supplier? Supplier { get; set; }
}
