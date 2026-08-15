namespace Bms.System.Application.Dtos.Messages;

/// <summary>
/// 内部服务触发消息发送参数（供其他服务调用）
/// </summary>
public class InternalMessageNotifyDto
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
    /// 来源子系统编码（关联 Subsystem.Code，如 "System"、"Store"）
    /// </summary>
    public string SourceSubsystemCode { get; set; } = string.Empty;

    /// <summary>
    /// 目标类型（1=用户, 2=角色, 3=组织, 5=全员）
    /// </summary>
    public int TargetType { get; set; }

    /// <summary>
    /// 目标ID列表
    /// </summary>
    public List<long> TargetIds { get; set; } = new();

    /// <summary>
    /// 跳转链接
    /// </summary>
    public string? TargetUrl { get; set; }

    /// <summary>
    /// 目标租户ID。后台服务无 HTTP 上下文，需显式指定消息归属租户。
    /// 未提供时由 NotifyAsync 使用当前请求租户（适用于 Web 请求内调用）。
    /// </summary>
    public long? TenantId { get; set; }

    /// <summary>
    /// 业务类型标识（如 "BirthdayReminder"），用于按业务场景去重。
    /// </summary>
    public string? BizType { get; set; }

    /// <summary>
    /// 业务唯一键（如 "{customerId}:{year}"），与 BizType 配合用于去重判断。
    /// </summary>
    public string? BizKey { get; set; }
}
