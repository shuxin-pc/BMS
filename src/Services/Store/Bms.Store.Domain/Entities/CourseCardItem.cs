namespace Bms.Store.Domain.Entities;

/// <summary>
/// 项目卡项目关联表
/// 项目卡包含的服务项目及其次数
/// </summary>
public class CourseCardItem : StoreEntity
{
    /// <summary>
    /// 项目卡ID
    /// </summary>
    public long CourseCardId { get; set; }

    /// <summary>
    /// 商品ID（关联实物或服务商品）
    /// </summary>
    public long ProductId { get; set; }

    /// <summary>
    /// 该项目在项目卡中的次数
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// 项目原价（购买时快照，用于折算计算）
    /// </summary>
    public decimal OriginalPrice { get; set; }

    /// <summary>
    /// 折算单价（购买时按项目卡售价比例分摊并锁定）
    /// 核销时按此单价计入营收
    /// </summary>
    public decimal AllocatedUnitPrice { get; set; }

    /// <summary>
    /// 分摊总价值（折算单价 × 数量）
    /// </summary>
    public decimal AllocatedTotalPrice { get; set; }

    /// <summary>
    /// 导航属性：项目卡
    /// </summary>
    public TreatmentCard? CourseCard { get; set; }

    /// <summary>
    /// 导航属性：商品
    /// </summary>
    public Product? Product { get; set; }
}
