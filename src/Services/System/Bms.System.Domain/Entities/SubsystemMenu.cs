namespace Bms.System.Domain.Entities;

/// <summary>
/// 子系统菜单关联实体
/// </summary>
public class SubsystemMenu : EntityBase
{
    /// <summary>
    /// 子系统ID
    /// </summary>
    public long SubsystemId { get; set; }

    /// <summary>
    /// 菜单ID
    /// </summary>
    public long MenuId { get; set; }

    /// <summary>
    /// 导航属性：子系统
    /// </summary>
    public virtual Subsystem? Subsystem { get; set; }

    /// <summary>
    /// 导航属性：菜单
    /// </summary>
    public virtual Menu? Menu { get; set; }
}
