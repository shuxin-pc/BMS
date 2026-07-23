namespace Bms.System.Application.Dtos.Messages;

/// <summary>
/// 消息已读/未读统计 DTO
/// </summary>
public class MessageReadStatsDto
{
    /// <summary>
    /// 总接收人数（排除已逻辑删除的接收记录）
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 已读人数
    /// </summary>
    public int ReadCount { get; set; }

    /// <summary>
    /// 未读人数
    /// </summary>
    public int UnreadCount { get; set; }

    /// <summary>
    /// 未读人员名单（前 50，按 UserId 升序）
    /// </summary>
    public List<MessageReadUserDto> UnreadList { get; set; } = new();
}
