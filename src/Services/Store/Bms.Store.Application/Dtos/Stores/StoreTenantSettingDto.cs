namespace Bms.Store.Application.Dtos.Stores;

/// <summary>
/// 门店设置 DTO
/// 跨店核销为租户级语义（读取时忽略门店，取租户第一条记录）
/// 各类型提醒接收角色已拆分至 StoreReminderSettingDto（按 ReminderType 循环返回）
/// </summary>
public class StoreTenantSettingDto
{
    public long Id { get; set; }

    /// <summary>
    /// 门店ID（当前门店上下文）
    /// </summary>
    public long StoreId { get; set; }

    /// <summary>
    /// 是否允许跨店核销（规则2，租户级）
    /// true-允许（默认），false-仅限发卡门店核销
    /// </summary>
    public bool AllowCrossStoreVerify { get; set; }

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
}
