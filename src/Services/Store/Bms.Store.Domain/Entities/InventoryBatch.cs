namespace Bms.Store.Domain.Entities;

/// <summary>
/// 库存批次表
/// 按批次管理库存，支持 FIFO 成本计算和效期管理
/// 配合 Inventory 汇总表使用
/// </summary>
public class InventoryBatch : StoreBusinessEntityBase
{
    /// <summary>
    /// 商品ID
    /// </summary>
    public long ProductId { get; set; }

    /// <summary>
    /// 批次号
    /// </summary>
    public string BatchNo { get; set; } = string.Empty;

    /// <summary>
    /// 当前批次库存数量
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// 采购单价（用于FIFO成本计算）
    /// </summary>
    public decimal UnitPrice { get; set; }

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
    /// 采购日期
    /// </summary>
    public DateTime? PurchaseDate { get; set; }

    /// <summary>
    /// 状态（1:在库 2:已用完 3:已过期）
    /// </summary>
    public int Status { get; set; } = 1;

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 导航属性：商品
    /// </summary>
    public Product? Product { get; set; }
}
