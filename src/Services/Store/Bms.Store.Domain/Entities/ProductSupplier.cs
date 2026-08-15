namespace Bms.Store.Domain.Entities;

/// <summary>
/// 品项-供应商 关联表（多对多）
/// 对应需求 G2.6.2：一个品项可关联多个供应商，一个供应商可供应多个品项
/// 通过 IsDefault 标识品项的默认供应商（Product.SupplierId 冗余字段已移除，IsDefault 为唯一权威源）
/// 关联表无软删除（解除关联即物理删除，避免历史数据干扰采购可选品项过滤）
/// </summary>
public class ProductSupplier : StoreBusinessEntityBase
{
    /// <summary>
    /// 商品ID（关联 Product.Id）
    /// </summary>
    public long ProductId { get; set; }

    /// <summary>
    /// 供应商ID（关联 Supplier.Id）
    /// </summary>
    public long SupplierId { get; set; }

    /// <summary>
    /// 是否默认供应商（一个品项仅一个默认，设置新默认时自动取消旧默认）
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// 参考采购价（用于采购下单时带出建议价，null=未设置）
    /// </summary>
    public decimal? ReferencePrice { get; set; }

    /// <summary>
    /// 供货周期（天，0=即时供货）
    /// </summary>
    public int LeadTimeDays { get; set; }

    /// <summary>
    /// 导航属性：商品
    /// </summary>
    public Product? Product { get; set; }

    /// <summary>
    /// 导航属性：供应商
    /// </summary>
    public Supplier? Supplier { get; set; }
}
