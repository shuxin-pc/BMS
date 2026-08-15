namespace Bms.System.Application.Dtos.Messages;

/// <summary>
/// 批量检查业务键是否存在请求（用于判断同一业务事件是否已发送消息，避免重复发送）
/// </summary>
public class BizKeyCheckDto
{
    /// <summary>
    /// 业务类型标识（如 "BirthdayReminder"）
    /// </summary>
    public string BizType { get; set; } = string.Empty;

    /// <summary>
    /// 待检查的业务唯一键列表（如 ["{customerId}:{year}", ...]）
    /// </summary>
    public List<string> BizKeys { get; set; } = new();
}
