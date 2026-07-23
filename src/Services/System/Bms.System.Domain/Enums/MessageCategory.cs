namespace Bms.System.Domain.Enums;

/// <summary>
/// 消息分类枚举
/// </summary>
public enum MessageCategory
{
    /// <summary>
    /// 系统通知（账号/角色/权限等管理类）
    /// </summary>
    System = 1,

    /// <summary>
    /// 业务通知（库存/采购/日结/会员等门店业务，含会员相关）
    /// </summary>
    Business = 2,

    /// <summary>
    /// 公告（管理员发布的公告）
    /// </summary>
    Announcement = 3
}
