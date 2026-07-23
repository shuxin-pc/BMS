namespace Bms.System.Domain.Entities;

/// <summary>
/// 消息接收记录实体
/// 继承 TenantEntity：含 IsDeleted 软删除 + TenantId/TenantCode 租户字段。
/// TenantId/TenantCode 为接收用户所在租户（与 Message.TenantId 可能不一致：
/// 跨租户消息场景下，如发给 tenant_admin 角色时，Message 归属平台租户，
/// 但接收用户散落在各个普通租户下，接收记录按用户所在租户写入，便于用户侧按自己租户查询收件箱）。
/// 用户侧通过 IsDeleted 软删除控制收件箱可见性。
/// </summary>
public class MessageRecipient : TenantEntity
{
    /// <summary>
    /// 消息ID（外键关联 Message）
    /// </summary>
    public long MessageId { get; set; }

    /// <summary>
    /// 接收用户ID
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 接收用户名（冗余存储，便于排查）
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// 是否已读
    /// </summary>
    public bool IsRead { get; set; }

    /// <summary>
    /// 已读时间
    /// </summary>
    public DateTime? ReadTime { get; set; }

    /// <summary>
    /// 删除时间（用户侧逻辑删除时记录）
    /// </summary>
    public DateTime? DeletedTime { get; set; }
}
