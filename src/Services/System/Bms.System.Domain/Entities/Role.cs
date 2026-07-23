namespace Bms.System.Domain.Entities;

/// <summary>
/// 角色实体
/// </summary>
public class Role : TenantEntity
{
    /// <summary>
    /// 角色名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 角色编码
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 是否系统角色（系统角色不允许删除和修改）
    /// </summary>
    public bool IsSystem { get; set; }

    /// <summary>
    /// 角色等级，数字越小权限越大
    /// 约定：super_admin=0, tenant_admin=1, 普通角色 > 1
    /// 用于约束角色分配与用户操作：只能分配/操作严格低于自身等级的角色
    /// 所有非 super_admin 角色均受 Level 约束：只能操作严格低于自身等级的角色
    /// </summary>
    public int Level { get; set; } = 100;

    /// <summary>
    /// 状态（0-禁用，1-正常）
    /// </summary>
    public int Status { get; set; } = 1;

    /// <summary>
    /// 导航属性：用户角色关联
    /// </summary>
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    /// <summary>
    /// 导航属性：角色权限关联
    /// </summary>
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();

    /// <summary>
    /// 导航属性：数据权限
    /// </summary>
    public virtual DataPermission? DataPermission { get; set; }
}