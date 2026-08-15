namespace Bms.Store.Application.Dtos.Suppliers;

/// <summary>
/// 品项-供应商关联 DTO
/// 对应需求 G2.6.2：展示品项与供应商的关联关系，含默认标识与参考采购价
/// </summary>
public class ProductSupplierDto
{
    public long Id { get; set; }

    /// <summary>
    /// 商品ID
    /// </summary>
    public long ProductId { get; set; }

    /// <summary>
    /// 商品编码（展示用）
    /// </summary>
    public string? ProductCode { get; set; }

    /// <summary>
    /// 商品名称（展示用）
    /// </summary>
    public string? ProductName { get; set; }

    /// <summary>
    /// 商品类型（1:实物商品 2:服务商品 3:耗材 4:样品 5:赠品）
    /// 用于采购订单按采购类型过滤商品下拉：零售商品采购->实物商品(1)，耗材采购->耗材(3)
    /// </summary>
    public int? ProductType { get; set; }

    /// <summary>
    /// 供应商ID
    /// </summary>
    public long SupplierId { get; set; }

    /// <summary>
    /// 供应商编码（展示用）
    /// </summary>
    public string? SupplierCode { get; set; }

    /// <summary>
    /// 供应商名称（展示用）
    /// </summary>
    public string? SupplierName { get; set; }

    /// <summary>
    /// 是否默认供应商
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// 参考采购价
    /// </summary>
    public decimal? ReferencePrice { get; set; }

    /// <summary>
    /// 供货周期（天）
    /// </summary>
    public int LeadTimeDays { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// 绑定品项到供应商入参 DTO
/// </summary>
public class BindProductsDto
{
    /// <summary>
    /// 供应商ID
    /// </summary>
    public long SupplierId { get; set; }

    /// <summary>
    /// 待绑定的品项ID列表
    /// </summary>
    public List<long> ProductIds { get; set; } = new();
}

/// <summary>
/// 设置默认供应商入参 DTO
/// </summary>
public class SetDefaultSupplierDto
{
    /// <summary>
    /// 商品ID
    /// </summary>
    public long ProductId { get; set; }

    /// <summary>
    /// 供应商ID
    /// </summary>
    public long SupplierId { get; set; }

    /// <summary>
    /// 参考采购价（可选，设置默认时可同步更新）
    /// </summary>
    public decimal? ReferencePrice { get; set; }

    /// <summary>
    /// 供货周期（天，可选）
    /// </summary>
    public int? LeadTimeDays { get; set; }
}

/// <summary>
/// 更新品项-供应商关联字段入参 DTO（仅更新参考价与供货周期，不改变默认供应商状态）
/// </summary>
public class UpdateProductSupplierDto
{
    /// <summary>
    /// 商品ID
    /// </summary>
    public long ProductId { get; set; }

    /// <summary>
    /// 供应商ID
    /// </summary>
    public long SupplierId { get; set; }

    /// <summary>
    /// 参考采购价
    /// </summary>
    public decimal? ReferencePrice { get; set; }

    /// <summary>
    /// 供货周期（天）
    /// </summary>
    public int? LeadTimeDays { get; set; }
}
