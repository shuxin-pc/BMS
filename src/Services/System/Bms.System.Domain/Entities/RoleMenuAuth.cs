namespace Bms.System.Domain.Entities;

/// <summary>
/// 角色菜单权限实体
/// </summary>
public class RoleMenuAuth : EntityBase
{
    /// <summary>
    /// 角色ID
    /// </summary>
    public long RoleId { get; set; }

    /// <summary>
    /// 菜单ID
    /// </summary>
    public long MenuId { get; set; }

    /// <summary>
    /// 子系统ID（冗余字段，用于按子系统分组展示）
    /// </summary>
    public long SubsystemId { get; set; }

    /// <summary>
    /// 导航属性：角色
    /// </summary>
    public virtual Role? Role { get; set; }

    /// <summary>
    /// 导航属性：菜单
    /// </summary>
    public virtual Menu? Menu { get; set; }

    /// <summary>
    /// 导航属性：子系统
    /// </summary>
    public virtual Subsystem? Subsystem { get; set; }
}
