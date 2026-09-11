namespace Bms.Store.Application.Dtos.Customers;

/// <summary>
/// 生日提醒输出 DTO
/// Id 字段填充客户ID，供前端标记关怀时回传
/// </summary>
public class BirthdayReminderDto
{
    /// <summary>
    /// 客户ID（作为列表项业务ID，标记关怀时回传）
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 客户ID
    /// </summary>
    public long CustomerId { get; set; }

    /// <summary>
    /// 客户姓名
    /// </summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>
    /// 手机号
    /// </summary>
    public string Phone { get; set; } = string.Empty;

    /// <summary>
    /// 生日（MM-dd 格式）
    /// </summary>
    public string Birthday { get; set; } = string.Empty;

    /// <summary>
    /// 距离生日天数
    /// </summary>
    public int DaysToBirthday { get; set; }

    /// <summary>
    /// 关怀状态：1=待关怀 2=已关怀
    /// </summary>
    public int CareStatus { get; set; }

    /// <summary>
    /// 关怀时间
    /// </summary>
    public DateTime? CareTime { get; set; }

    /// <summary>
    /// 操作人姓名（已关怀时显示）
    /// </summary>
    public string? OperatorName { get; set; }
}
