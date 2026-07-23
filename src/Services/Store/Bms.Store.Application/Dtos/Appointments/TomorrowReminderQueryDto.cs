using Bms.Store.Application.Dtos;

namespace Bms.Store.Application.Dtos.Appointments;

/// <summary>
/// 预约明日提醒查询参数
/// </summary>
public class TomorrowReminderQueryDto : PagedRequestDto
{
    /// <summary>
    /// 客户名称（模糊匹配）
    /// </summary>
    public string? CustomerName { get; set; }

    /// <summary>
    /// 手机号（模糊匹配）
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// 提醒状态（1:待提醒 2:已提醒）
    /// </summary>
    public int? RemindStatus { get; set; }
}
