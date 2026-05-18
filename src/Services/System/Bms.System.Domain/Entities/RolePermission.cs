namespace Bms.System.Domain.Entities;

/// <summary>
/// 角色权限关联实体
/// </summary>
public class RolePermission : EntityBase
{
    /// <summary>
    /// 角色ID
    /// </summary>
    public long RoleId { get; set; }

    /// <summary>
    /// 权限ID
    /// </summary>
    public long PermissionId { get; set; }

    /// <summary>
    /// 导航属性：角色
    /// </summary>
    public virtual Role? Role { get; set; }

    /// <summary>
    /// 导航属性：权限
    /// </summary>
    public virtual Permission? Permission { get; set; }
}