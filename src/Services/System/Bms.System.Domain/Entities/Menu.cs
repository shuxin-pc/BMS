namespace Bms.System.Domain.Entities;

/// <summary>
/// 菜单实体
/// </summary>
public class Menu : BaseEntity
{
    /// <summary>
    /// 父级ID（null表示顶级）
    /// </summary>
    public long? ParentId { get; set; }

    /// <summary>
    /// 菜单名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 菜单编码
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 路由路径
    /// </summary>
    public string? Path { get; set; }

    /// <summary>
    /// 组件路径
    /// </summary>
    public string? Component { get; set; }

    /// <summary>
    /// 图标
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// 排序
    /// </summary>
    public int Sort { get; set; }

    /// <summary>
    /// 菜单类型（0-目录，1-菜单，2-按钮）
    /// </summary>
    public int Type { get; set; }

    /// <summary>
    /// 状态（0-禁用，1-正常）
    /// </summary>
    public int Status { get; set; } = 1;

    /// <summary>
    /// 权限编码
    /// </summary>
    public string? PermissionCode { get; set; }

    /// <summary>
    /// 是否可见
    /// </summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>
    /// 是否缓存
    /// </summary>
    public bool IsCache { get; set; } = true;

    /// <summary>
    /// 是否总是显示
    /// </summary>
    public bool IsAlwaysShow { get; set; }

    /// <summary>
    /// 导航属性：子菜单
    /// </summary>
    public virtual ICollection<Menu> Children { get; set; } = new List<Menu>();

    /// <summary>
    /// 导航属性：父菜单
    /// </summary>
    public virtual Menu? Parent { get; set; }
}