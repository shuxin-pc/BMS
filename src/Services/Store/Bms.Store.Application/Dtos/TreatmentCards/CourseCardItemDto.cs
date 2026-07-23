namespace Bms.Store.Application.Dtos.TreatmentCards;

/// <summary>
/// 疗程卡项目关联表 DTO
/// </summary>
public class CourseCardItemDto
{
    public long Id { get; set; }

    /// <summary>
    /// 疗程卡ID
    /// </summary>
    public long CourseCardId { get; set; }

    /// <summary>
    /// 商品ID
    /// </summary>
    public long ProductId { get; set; }

    /// <summary>
    /// 该项目在疗程卡中的次数
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// 项目原价
    /// </summary>
    public decimal OriginalPrice { get; set; }

    /// <summary>
    /// 折算单价
    /// </summary>
    public decimal AllocatedUnitPrice { get; set; }

    /// <summary>
    /// 分摊总价值
    /// </summary>
    public decimal AllocatedTotalPrice { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}
