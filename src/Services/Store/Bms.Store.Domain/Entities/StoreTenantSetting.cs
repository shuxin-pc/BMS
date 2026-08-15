namespace Bms.Store.Domain.Entities;

/// <summary>
/// 租户级业务配置（每租户一条记录）
/// 用于存储跨店核销、转让等租户级开关与限制
/// </summary>
public class StoreTenantSetting : StoreTenantEntity
{
    /// <summary>
    /// 是否允许跨店核销（MVP 默认开启）
    /// 关闭后仅允许在发卡门店核销
    /// </summary>
    public bool AllowCrossStoreVerify { get; set; } = true;

    /// <summary>
    /// 接收客户生日提醒站内信的角色ID列表（空列表表示不发送）
    /// 角色为租户级，此处存储角色ID，发送时由 System 服务解析
    /// </summary>
    public List<long> BirthdayReminderRoleIds { get; set; } = new();
}
