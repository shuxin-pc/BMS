namespace Bms.System.Domain.Enums;

/// <summary>
/// 用户状态枚举
/// </summary>
public enum UserStatus
{
    /// <summary>
    /// 禁用
    /// </summary>
    Disabled = 0,

    /// <summary>
    /// 正常
    /// </summary>
    Normal = 1
}

/// <summary>
/// 菜单类型枚举
/// </summary>
public enum MenuType
{
    /// <summary>
    /// 目录
    /// </summary>
    Directory = 0,

    /// <summary>
    /// 菜单
    /// </summary>
    Menu = 1,

    /// <summary>
    /// 按钮
    /// </summary>
    Button = 2
}

/// <summary>
/// 组织类型枚举
/// </summary>
public enum OrganizationType
{
    /// <summary>
    /// 公司
    /// </summary>
    Company = 0,

    /// <summary>
    /// 部门
    /// </summary>
    Department = 1,

    /// <summary>
    /// 班组
    /// </summary>
    Team = 2
}