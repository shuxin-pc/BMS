namespace Bms.System.Domain.Entities;

/// <summary>
/// 子系统实体
/// </summary>
public class Subsystem : BaseEntity
{
    /// <summary>
    /// 子系统编码（唯一）
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 子系统名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 图标
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// 描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 排序号
    /// </summary>
    public int Sort { get; set; } = 0;

    /// <summary>
    /// 状态：0-禁用，1-启用
    /// </summary>
    public int Status { get; set; } = 1;

    /// <summary>
    /// 导航属性：子系统关联的菜单
    /// </summary>
    public virtual ICollection<SubsystemMenu> SubsystemMenus { get; set; } = new List<SubsystemMenu>();

    /// <summary>
    /// 导航属性：子系统关联的租户
    /// </summary>
    public virtual ICollection<TenantSubsystem> TenantSubsystems { get; set; } = new List<TenantSubsystem>();

    /// <summary>
    /// 导航属性：子系统关联的角色菜单权限
    /// </summary>
    public virtual ICollection<RoleMenuAuth> RoleMenuAuths { get; set; } = new List<RoleMenuAuth>();
}
