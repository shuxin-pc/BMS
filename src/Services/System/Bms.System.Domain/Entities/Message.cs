using Bms.System.Domain.Enums;

namespace Bms.System.Domain.Entities;

/// <summary>
/// 消息发送记录实体
/// 不实现 ITenant 接口：跨租户消息时 TenantId 为目标租户ID（非发送者租户），
/// 由业务逻辑显式赋值，避免 TenantQueryFilterInterceptor 自动填充干扰。
/// 管理端通过 IsRecalled 标识撤回状态，不使用软删除。
/// </summary>
public class Message : EntityBase
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
    /// 消息分类
    /// </summary>
    public MessageCategory Category { get; set; }

    /// <summary>
    /// 来源类型：1=自动触发, 2=手动推送
    /// </summary>
    public MessageSourceType SourceType { get; set; }

    /// <summary>
    /// 来源子系统编码（关联 Subsystem.Code，如 System、Store、Identity）
    /// </summary>
    public string SourceSubsystemCode { get; set; } = string.Empty;

    /// <summary>
    /// 目标类型：1=用户, 2=角色, 3=组织, 4=租户, 5=全员
    /// </summary>
    public MessageTargetType TargetType { get; set; }

    /// <summary>
    /// 目标ID列表（逗号分隔；全员时为空）。仅用于管理端展示，不参与接收者查询。
    /// </summary>
    public string TargetIds { get; set; } = string.Empty;

    /// <summary>
    /// 目标描述（如"租户A,租户B"或"采购经理,店长"），用于管理端展示
    /// </summary>
    public string TargetDesc { get; set; } = string.Empty;

    /// <summary>
    /// 发送者ID（手动推送时记录，自动触发为空）
    /// </summary>
    public long? SenderId { get; set; }

    /// <summary>
    /// 发送者名称
    /// </summary>
    public string SenderName { get; set; } = string.Empty;

    /// <summary>
    /// 跳转链接（如 /store/inventory）
    /// </summary>
    public string TargetUrl { get; set; } = string.Empty;

    /// <summary>
    /// 是否跨租户消息（平台发送时为 true）
    /// </summary>
    public bool IsCrossTenant { get; set; }

    /// <summary>
    /// 是否已撤回（管理端撤回时设为 true）
    /// </summary>
    public bool IsRecalled { get; set; }

    /// <summary>
    /// 消息归属租户ID。租户内消息时为发送者所在租户；跨租户消息时为目标租户ID（每个目标租户一条 Message）。
    /// </summary>
    public long TenantId { get; set; }

    /// <summary>
    /// 消息归属租户编码（与 TenantId 对应）
    /// </summary>
    public string TenantCode { get; set; } = string.Empty;
}
