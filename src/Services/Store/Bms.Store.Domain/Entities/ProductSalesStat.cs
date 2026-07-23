namespace Bms.Store.Domain.Entities;

/// <summary>
/// 商品销售统计表
/// 按日/月聚合商品销售数据，支撑首页热门商品/服务 TOP5 排行
/// </summary>
public class ProductSalesStat : StoreBusinessEntityBase
{
    /// <summary>
    /// 统计日期
    /// </summary>
    public DateTime StatDate { get; set; }

    /// <summary>
    /// 统计月份（yyyy-MM，用于月度聚合）
    /// </summary>
    public string StatMonth { get; set; } = string.Empty;

    /// <summary>
    /// 商品ID
    /// </summary>
    public long ProductId { get; set; }

    /// <summary>
    /// 商品名称（冗余存储）
    /// </summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// 商品类型（1:零售 2:服务 3:耗材 4:疗程卡购买 5:疗程卡核销）
    /// 疗程卡购买（4）计入服务排行；核销（5）为权责发生制转营收，不计入销售排行
    /// </summary>
    public int ProductType { get; set; }

    /// <summary>
    /// 销售次数
    /// </summary>
    public int SalesCount { get; set; }

    /// <summary>
    /// 销售金额
    /// </summary>
    public decimal SalesAmount { get; set; }

    /// <summary>
    /// 导航属性：商品
    /// </summary>
    public Product? Product { get; set; }
}
