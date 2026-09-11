namespace Bms.Store.Application.Dtos.Stores;

/// <summary>
/// 门店提醒配置 DTO（对应子表 StoreReminderSetting）
/// 每门店 + 每业务类型一行，通用化承载各类提醒的接收角色
/// </summary>
public class StoreReminderSettingDto
{
    /// <summary>
    /// 提醒业务类型编码（Birthday=生日、Appointment=预约）
    /// </summary>
    public string ReminderType { get; set; } = string.Empty;

    /// <summary>
    /// 接收该类型提醒站内信的角色ID列表（空列表表示不发送）
    /// 通过 LongToStringConverter 序列化为字符串数组，避免 JS 精度丢失
    /// </summary>
    public List<long> RoleIds { get; set; } = new();
}

/// <summary>
/// 更新门店提醒配置请求 DTO（按门店批量保存多类型配置，upsert 到子表）
/// </summary>
public class StoreReminderSettingUpdateDto
{
    /// <summary>
    /// 提醒业务类型编码（Birthday=生日、Appointment=预约）
    /// </summary>
    public string ReminderType { get; set; } = string.Empty;

    /// <summary>
    /// 接收该类型提醒站内信的角色ID列表（空列表表示不发送）
    /// </summary>
    public List<long> RoleIds { get; set; } = new();
}
