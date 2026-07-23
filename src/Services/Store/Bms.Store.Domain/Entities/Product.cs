namespace Bms.Store.Domain.Entities;

/// <summary>
/// 商品档案
/// </summary>
public class Product : StoreEntity
{
    /// <summary>
    /// 商品名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 商品编码
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 商品类型（1:实物商品 2:服务商品 3:耗材 4:样品 5:赠品）
    /// </summary>
    public int Type { get; set; } = 1;

    /// <summary>
    /// 商品分类ID
    /// </summary>
    public long CategoryId { get; set; }

    /// <summary>
    /// 单位（如：个、瓶、盒）
    /// </summary>
    public string? Unit { get; set; }

    /// <summary>
    /// 零售价
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// 成本价
    /// </summary>
    public decimal? CostPrice { get; set; }

    /// <summary>
    /// 上次采购价（采购入库时自动更新）
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
    /// 规格
    /// </summary>
    public string? Specification { get; set; }

    /// <summary>
    /// 品牌
    /// </summary>
    public string? Brand { get; set; }

    /// <summary>
    /// 供应商ID
    /// </summary>
    public long? SupplierId { get; set; }

    /// <summary>
    /// 商品状态（1:上架 2:下架）
    /// </summary>
    public int Status { get; set; } = 1;

    /// <summary>
    /// 是否可销售（true=可通过 POS 销售下单；false=样品/赠品等不可销售品项）
    /// 类型为 4(样品)/5(赠品) 时强制为 false，其他类型默认为 true
    /// 依据：B6.1 "设置「不可销售」标识"
    /// </summary>
    public bool IsSalable { get; set; } = true;

    /// <summary>
    /// 商品图片URL
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 导航属性：分类
    /// </summary>
    public ProductCategory? Category { get; set; }
}
