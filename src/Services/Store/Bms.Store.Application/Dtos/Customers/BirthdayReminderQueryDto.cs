using Bms.Store.Application.Dtos;

namespace Bms.Store.Application.Dtos.Customers;

/// <summary>
/// 生日提醒分页查询参数
/// </summary>
public class BirthdayReminderQueryDto : PagedRequestDto
{
    /// <summary>
    /// 客户姓名（模糊匹配）
    /// </summary>
    public string? CustomerName { get; set; }

    /// <summary>
    /// 手机号（模糊匹配）
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// 关怀状态：1=待关怀 2=已关怀
    /// </summary>
    public int? CareStatus { get; set; }
}
