namespace Bms.Store.Domain.Constants;

/// <summary>
/// 提醒业务类型编码常量
/// 与 StoreReminderSetting.ReminderType 字段对应，集中管理避免魔法字符串
/// </summary>
public static class ReminderTypes
{
    /// <summary>生日提醒（客户生日关怀站内信）</summary>
    public const string Birthday = "Birthday";

    /// <summary>预约提醒（今日预约安排站内信，每日上班前 08:00 送达）</summary>
    public const string Appointment = "Appointment";

    /// <summary>项目卡到期提醒（即将到期/已过期项目卡站内信，每日 08:30 送达）</summary>
    public const string TreatmentExpiry = "TreatmentExpiry";
}
