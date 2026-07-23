using Bms.Store.Application.Dtos;

namespace Bms.Store.Application.Dtos.TreatmentCards;

/// <summary>
/// 疗程卡到期提醒查询参数
/// </summary>
public class TreatmentCardExpiryQueryDto : PagedRequestDto
{
    /// <summary>
    /// 客户名称（模糊匹配）
    /// </summary>
    public string? CustomerName { get; set; }

    /// <summary>
    /// 预警级别（1:即将到期 2:已到期）
    /// </summary>
    public int? AlertLevel { get; set; }
}
