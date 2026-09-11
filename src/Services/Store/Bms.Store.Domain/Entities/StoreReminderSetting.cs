namespace Bms.Store.Domain.Entities;

/// <summary>
/// 门店提醒配置（每门店 + 每业务类型一行）
/// 通用化替代原 StoreTenantSettings.BirthdayReminderRoleIds 列：
/// - 主表 StoreTenantSettings 懒创建可能无行，本子表需独立存在，用业务键 TenantId + StoreId 过滤（不存主表外键）
/// - 后台服务按 TenantId + StoreId 扫描业务数据，按 ReminderType 取对应接收角色
/// </summary>
public class StoreReminderSetting : StoreEntity
{
    /// <summary>
    /// 提醒业务类型编码（Birthday=生日、Appointment=预约，后续可扩展 TreatmentExpiry/Inventory 等）
    /// </summary>
    public string ReminderType { get; set; } = string.Empty;

    /// <summary>
    /// 接收该类型提醒站内信的角色ID列表（空列表=不发送，jsonb 数组）
    /// 角色为租户级，此处存储角色ID，发送时由 System 服务解析
    /// </summary>
    public List<long> RoleIds { get; set; } = new();
}
