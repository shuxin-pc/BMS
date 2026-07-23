namespace Bms.Store.Application.Dtos.TreatmentCards;

/// <summary>
/// 创建疗程卡项目明细请求 DTO
/// 前端只传入商品、次数、原价；折算单价由后端按卡价比例分摊计算
/// </summary>
public class CourseCardItemCreateDto
{
    /// <summary>
    /// 商品ID（服务项目）
    /// </summary>
    public long ProductId { get; set; }

    /// <summary>
    /// 该项目在疗程卡中的次数
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// 项目原价（用于折算计算）
    /// </summary>
    public decimal OriginalPrice { get; set; }
}
