namespace Bms.System.Domain.Enums;

/// <summary>
/// 消息目标类型枚举
/// </summary>
public enum MessageTargetType
{
    /// <summary>
    /// 指定用户
    /// </summary>
    User = 1,

    /// <summary>
    /// 按角色
    /// </summary>
    Role = 2,

    /// <summary>
    /// 按组织
    /// </summary>
    Organization = 3,

    /// <summary>
    /// 指定租户（仅平台管理员可用）
    /// </summary>
    Tenant = 4,

    /// <summary>
    /// 全员（当前租户内所有用户）
    /// </summary>
    All = 5
}
