namespace Bms.Store.Domain.Entities;

/// <summary>
/// 门店业务配置（每门店一条记录）
/// 存储跨店核销（租户级语义，忽略门店）等配置
/// 各类型提醒接收角色已通用化拆分至子表 StoreReminderSetting（每门店+每业务类型一行）
/// </summary>
public class StoreTenantSetting : StoreEntity
{
    /// <summary>
    /// 是否允许跨店核销（MVP 默认开启）
    /// 关闭后仅允许在发卡门店核销
    /// </summary>
    public bool AllowCrossStoreVerify { get; set; } = true;
}
