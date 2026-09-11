using Bms.Store.Application.Dtos;

namespace Bms.Store.Application.Dtos.Customers;

/// <summary>
/// 生日提醒分页查询参数
/// </summary>
public class BirthdayReminderQueryDto : PagedRequestDto
{
    /// <summary>
    /// 客户名称或手机号关键字（模糊匹配，OR 语义：命中姓名或手机号其一即满足）
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// 关怀状态：1=待关怀 2=已关怀
    /// </summary>
    public int? CareStatus { get; set; }
}
