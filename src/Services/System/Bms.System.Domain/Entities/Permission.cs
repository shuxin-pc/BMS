namespace Bms.System.Domain.Entities;

/// <summary>
/// 权限实体
/// </summary>
public class Permission : TenantEntity
{
    /// <summary>
    /// 权限名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 权限编码
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 关联菜单ID
    /// </summary>
    public long MenuId { get; set; }

    /// <summary>
    /// HTTP方法（GET、POST、PUT、DELETE）
    /// </summary>
    public string? HttpMethod { get; set; }

    /// <summary>
    /// API路径
    /// </summary>
    public string? ApiPath { get; set; }

    /// <summary>
    /// 导航属性：菜单
    /// </summary>
    public virtual Menu? Menu { get; set; }

    /// <summary>
    /// 导航属性：角色权限关联
    /// </summary>
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}