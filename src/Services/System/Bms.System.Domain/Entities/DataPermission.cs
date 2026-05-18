namespace Bms.System.Domain.Entities;

/// <summary>
/// 数据权限实体
/// </summary>
public class DataPermission : BaseEntity
{
    /// <summary>
    /// 角色ID
    /// </summary>
    public long RoleId { get; set; }

    /// <summary>
    /// 数据范围类型（1-全部，2-部门及以下，3-仅本人，4-自定义）
    /// </summary>
    public int DataScopeType { get; set; } = 1;

    /// <summary>
    /// 自定义组织ID列表（逗号分隔）
    /// </summary>
    public string? CustomOrganizationIds { get; set; }

    /// <summary>
    /// 导航属性：角色
    /// </summary>
    public virtual Role? Role { get; set; }
}