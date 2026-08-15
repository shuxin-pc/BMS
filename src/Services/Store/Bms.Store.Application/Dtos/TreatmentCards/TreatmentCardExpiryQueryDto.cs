using Bms.Store.Application.Dtos;

namespace Bms.Store.Application.Dtos.TreatmentCards;

/// <summary>
/// 疗程卡到期提醒查询参数
/// 查询视角（文档 4.2）：客户视角（疗程卡到期提醒按客户视角查询所有门店的有效卡）
/// AppService 按 TenantId + 客户名称模糊匹配过滤，不按 StoreId 过滤（跨店购卡均可见）
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
