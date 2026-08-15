namespace Bms.Store.Application.Dtos.Stores;

/// <summary>
/// 租户门店设置 DTO
/// </summary>
public class StoreTenantSettingDto
{
    public long Id { get; set; }

    /// <summary>
    /// 是否允许跨店核销（规则2）
    /// true-允许（默认），false-仅限发卡门店核销
    /// </summary>
    public bool AllowCrossStoreVerify { get; set; }

    /// <summary>
    /// 接收生日提醒站内信的角色ID列表（空列表表示不发送）
    /// 通过 LongToStringConverter 序列化为字符串数组，避免 JS 精度丢失
    /// </summary>
    public List<long> BirthdayReminderRoleIds { get; set; } = new();

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// 更新租户门店设置 DTO
/// </summary>
public class StoreTenantSettingUpdateDto
{
    /// <summary>
    /// 是否允许跨店核销
    /// </summary>
    public bool AllowCrossStoreVerify { get; set; }

    /// <summary>
    /// 接收生日提醒站内信的角色ID列表（空列表表示不发送）
    /// </summary>
    public List<long> BirthdayReminderRoleIds { get; set; } = new();
}
