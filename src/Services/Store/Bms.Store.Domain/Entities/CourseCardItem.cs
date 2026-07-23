namespace Bms.Store.Domain.Entities;

/// <summary>
/// 疗程卡项目关联表
/// 疗程卡包含的服务项目及其次数
/// </summary>
public class CourseCardItem : StoreEntity
{
    /// <summary>
    /// 疗程卡ID
    /// </summary>
    public long CourseCardId { get; set; }

    /// <summary>
    /// 商品ID（关联实物或服务商品）
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
    /// 折算单价（购买时按疗程卡售价比例分摊并锁定）
    /// 核销时按此单价计入营收
    /// </summary>
    public decimal AllocatedUnitPrice { get; set; }

    /// <summary>
    /// 分摊总价值（折算单价 × 数量）
    /// </summary>
    public decimal AllocatedTotalPrice { get; set; }

    /// <summary>
    /// 导航属性：疗程卡
    /// </summary>
    public TreatmentCard? CourseCard { get; set; }

    /// <summary>
    /// 导航属性：商品
    /// </summary>
    public Product? Product { get; set; }
}
