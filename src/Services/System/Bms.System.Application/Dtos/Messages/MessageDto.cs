namespace Bms.System.Application.Dtos.Messages;

/// <summary>
/// 管理端消息发送记录 DTO
/// </summary>
public class MessageDto
{
    /// <summary>
    /// 消息ID
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 消息标题
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 消息内容
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 分类
    /// </summary>
    public int Category { get; set; }

    /// <summary>
    /// 分类名称
    /// </summary>
    public string CategoryName { get; set; } = string.Empty;

    /// <summary>
    /// 来源类型（1=自动, 2=手动）
    /// </summary>
    public int SourceType { get; set; }

    /// <summary>
    /// 来源类型名称
    /// </summary>
    public string SourceTypeName { get; set; } = string.Empty;

    /// <summary>
    /// 来源子系统编码
    /// </summary>
    public string SourceSubsystemCode { get; set; } = string.Empty;

    /// <summary>
    /// 来源子系统名称
    /// </summary>
    public string SourceSubsystemName { get; set; } = string.Empty;

    /// <summary>
    /// 目标类型
    /// </summary>
    public int TargetType { get; set; }

    /// <summary>
    /// 目标类型名称
    /// </summary>
    public string TargetTypeName { get; set; } = string.Empty;

    /// <summary>
    /// 目标描述
    /// </summary>
    public string TargetDesc { get; set; } = string.Empty;

    /// <summary>
    /// 发送者ID
    /// </summary>
    public long? SenderId { get; set; }

    /// <summary>
    /// 发送者名称
    /// </summary>
    public string SenderName { get; set; } = string.Empty;

    /// <summary>
    /// 跳转链接
    /// </summary>
    public string TargetUrl { get; set; } = string.Empty;

    /// <summary>
    /// 是否跨租户
    /// </summary>
    public bool IsCrossTenant { get; set; }

    /// <summary>
    /// 归属租户名称（与 TenantId 对应，便于管理端展示）
    /// </summary>
    public string TenantName { get; set; } = string.Empty;

    /// <summary>
    /// 是否已撤回
    /// </summary>
    public bool IsRecalled { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedTime { get; set; }
}
