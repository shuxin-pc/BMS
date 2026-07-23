namespace Bms.System.Domain.Enums;

/// <summary>
/// 消息来源类型枚举
/// </summary>
public enum MessageSourceType
{
    /// <summary>
    /// 自动触发（业务事件）
    /// </summary>
    Auto = 1,

    /// <summary>
    /// 手动推送（管理员）
    /// </summary>
    Manual = 2
}
