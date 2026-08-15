namespace Bms.Store.Domain.Entities;

/// <summary>
/// 门店商品档案（门店隔离，承载分店差异化属性，每店一份）
/// 对应设计文档 3.2 节
/// Master 字段（编码/名称/类型/分类等本质属性）已移至 ProductMaster，通过 MasterId 关联
/// 唯一约束：TenantId + StoreId + MasterId（同一门店同一主档只能有一份档案）
/// </summary>
public class Product : StoreEntity
{
    /// <summary>
    /// 关联商品主档ID（引用 ProductMaster.Id）
    /// </summary>
    public long MasterId { get; set; }

    /// <summary>
    /// 零售价（分店独立定价）
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// 成本价（分店独立）
    /// </summary>
    public decimal? CostPrice { get; set; }

    /// <summary>
    /// 上次采购价（采购入库时自动更新，分店独立采购）
    /// </summary>
    public decimal? LastPurchasePrice { get; set; }

    /// <summary>
    /// 低库存预警阈值（低于此值触发预警，null=不预警）
    /// </summary>
    public decimal? LowStockThreshold { get; set; }

    /// <summary>
    /// 效期预警天数（剩余天数小于等于此值触发预警，null=不预警）
    /// </summary>
    public int? ExpiryAlertDays { get; set; }

    /// <summary>
    /// 积压预警阈值（超过此值触发预警，null=不预警）
    /// </summary>
    public decimal? OverstockThreshold { get; set; }

    /// <summary>
    /// 商品状态（1:上架 2:下架，分店选择性上架）
    /// </summary>
    public int Status { get; set; } = 1;

    /// <summary>
    /// 分店级备注
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 导航属性：商品主档
    /// </summary>
    public ProductMaster? Master { get; set; }
}
