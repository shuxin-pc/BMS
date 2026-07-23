namespace Bms.System.Application.Dtos.Messages;

/// <summary>
/// 收件箱项 DTO（用户侧视角）
/// </summary>
public class MessageInboxDto
{
    /// <summary>
    /// 接收记录ID（MessageRecipient.Id）
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 消息ID
    /// </summary>
    public long MessageId { get; set; }

    /// <summary>
    /// 消息标题
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 消息内容
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 分类（1=系统, 2=业务, 3=公告）
    /// </summary>
    public int Category { get; set; }

    /// <summary>
    /// 分类名称
    /// </summary>
    public string CategoryName { get; set; } = string.Empty;

    /// <summary>
    /// 发送者名称
    /// </summary>
    public string SenderName { get; set; } = string.Empty;

    /// <summary>
    /// 跳转链接
    /// </summary>
    public string TargetUrl { get; set; } = string.Empty;

    /// <summary>
    /// 是否已读
    /// </summary>
    public bool IsRead { get; set; }

    /// <summary>
    /// 已读时间
    /// </summary>
    public DateTime? ReadTime { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedTime { get; set; }
}
