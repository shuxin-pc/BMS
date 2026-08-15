namespace Bms.Store.Domain.Entities;

/// <summary>
/// 疗程卡销售项目明细（购买时按实际售价分摊计算折算单价并锁定）
/// </summary>
public class TreatmentCardSaleItem : StoreBusinessEntityBase
{
    /// <summary>
    /// 疗程卡销售记录ID
    /// </summary>
    public long SaleId { get; set; }

    /// <summary>
    /// 商品ID（服务项目）
    /// </summary>
    public long ProductId { get; set; }

    /// <summary>
    /// 该项目在疗程卡中的次数
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// 项目原价（购买时快照，用于折算计算）
    /// </summary>
    public decimal OriginalPrice { get; set; }

    /// <summary>
    /// 折算单价（购买时按疗程卡实际售价比例分摊并锁定，核销时按此单价计入营收）
    /// </summary>
    public decimal AllocatedUnitPrice { get; set; }

    /// <summary>
    /// 分摊总价值（折算单价 × 数量）
    /// </summary>
    public decimal AllocatedTotalPrice { get; set; }

    /// <summary>
    /// 导航属性：疗程卡销售记录
    /// </summary>
    public TreatmentCardSale? Sale { get; set; }

    /// <summary>
    /// 导航属性：商品
    /// </summary>
    public Product? Product { get; set; }
}
