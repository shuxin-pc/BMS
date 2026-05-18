namespace Bms.System.Domain.Enums;

/// <summary>
/// 数据范围类型枚举
/// </summary>
public enum DataScopeType
{
    /// <summary>
    /// 全部数据
    /// </summary>
    All = 1,

    /// <summary>
    /// 本部门及以下
    /// </summary>
    DepartmentAndBelow = 2,

    /// <summary>
    /// 仅本人
    /// </summary>
    Self = 3,

    /// <summary>
    /// 自定义（指定组织）
    /// </summary>
    Custom = 4
}