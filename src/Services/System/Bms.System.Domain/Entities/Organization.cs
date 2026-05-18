namespace Bms.System.Domain.Entities;

/// <summary>
/// 组织架构实体
/// </summary>
public class Organization : TenantEntity
{
    /// <summary>
    /// 父级ID（null表示顶级）
    /// </summary>
    public long? ParentId { get; set; }

    /// <summary>
    /// 组织名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 组织编码
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 组织类型（0-公司，1-部门，2-班组）
    /// </summary>
    public int Type { get; set; }

    /// <summary>
    /// 负责人ID
    /// </summary>
    public long? ManagerId { get; set; }

    /// <summary>
    /// 状态（0-禁用，1-正常）
    /// </summary>
    public int Status { get; set; } = 1;

    /// <summary>
    /// 排序
    /// </summary>
    public int Sort { get; set; }

    /// <summary>
    /// 联系电话
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// 联系地址
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// 导航属性：子组织
    /// </summary>
    public virtual ICollection<Organization> Children { get; set; } = new List<Organization>();

    /// <summary>
    /// 导航属性：父组织
    /// </summary>
    public virtual Organization? Parent { get; set; }

    /// <summary>
    /// 导航属性：用户
    /// </summary>
    public virtual ICollection<User> Users { get; set; } = new List<User>();
}