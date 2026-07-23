namespace Bms.System.Application.Dtos.Messages;

/// <summary>
/// 发送消息参数（管理端手动推送）
/// </summary>
public class MessageSendDto
{
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
    /// 目标类型（1=用户, 2=角色, 3=组织, 4=租户, 5=全员）
    /// </summary>
    public int TargetType { get; set; }

    /// <summary>
    /// 目标ID列表（全员时为空；按租户发送时为租户ID列表）
    /// </summary>
    public List<long> TargetIds { get; set; } = new();

    /// <summary>
    /// 跨租户发送时的目标租户列表（仅平台管理员可用，配合 TargetType=Role/Organization/User 使用）
    /// </summary>
    public List<long>? TargetTenantIds { get; set; }

    /// <summary>
    /// 跳转链接（可选）
    /// </summary>
    public string? TargetUrl { get; set; }
}
