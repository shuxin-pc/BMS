namespace Bms.Store.Domain.Entities;

/// <summary>
/// 商品主档（租户级共享，承载商品本质属性，全租户一份）
/// 对应设计文档 3.1 节
/// 与 Product（门店档案）拆分后，Master 承载跨店一致的属性，消除"识别同款"与"多份档案"问题
/// </summary>
public class ProductMaster : StoreTenantEntity
{
    /// <summary>
    /// 商品编码（租户内唯一，核心识别键）
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 商品名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 商品类型（1:实物商品 2:服务商品 3:耗材 4:样品 5:赠品）
    /// </summary>
    public int Type { get; set; } = 1;

    /// <summary>
    /// 商品分类ID（引用租户级 ProductCategory.Id）
    /// </summary>
    public long CategoryId { get; set; }

    /// <summary>
    /// 单位（如：个、瓶、盒）
    /// </summary>
    public string? Unit { get; set; }

    /// <summary>
    /// 规格
    /// </summary>
    public string? Specification { get; set; }

    /// <summary>
    /// 品牌
    /// </summary>
    public string? Brand { get; set; }

    /// <summary>
    /// 商品图片URL
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// 是否可销售（true=可通过 POS 销售下单；false=样品/赠品等不可销售品项）
    /// 类型为 4(样品)/5(赠品) 时强制为 false，其他类型默认为 true
    /// 依据：设计文档 4.1 节"IsSalable 跟 Type 走"
    /// </summary>
    public bool IsSalable { get; set; } = true;

    /// <summary>
    /// 主档级备注
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 导航属性：分类（租户级 ProductCategory）
    /// </summary>
    public ProductCategory? Category { get; set; }

    /// <summary>
    /// 导航属性：服务商品子表（仅 Type=2 服务商品时有值）
    /// </summary>
    public ServiceProduct? ServiceProduct { get; set; }
}
