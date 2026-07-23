namespace Bms.System.Domain.Entities;

/// <summary>
/// 用户实体
/// </summary>
public class User : TenantEntity
{
    /// <summary>
    /// 用户名
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// 真实姓名
    /// </summary>
    public string RealName { get; set; } = string.Empty;

    /// <summary>
    /// 邮箱
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// 手机号
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// 密码哈希
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// 头像URL
    /// </summary>
    public string? Avatar { get; set; }

    /// <summary>
    /// 状态（0-禁用，1-正常）
    /// </summary>
    public int Status { get; set; } = 1;

    /// <summary>
    /// 最后登录时间
    /// </summary>
    public DateTime? LastLoginTime { get; set; }

    /// <summary>
    /// 最后登录IP
    /// </summary>
    public string? LastLoginIp { get; set; }

    /// <summary>
    /// 组织ID
    /// </summary>
    public long? OrganizationId { get; set; }

    /// <summary>
    /// 创建者所属租户ID
    /// 用于区分平台租户跨租户创建的用户：当本字段不等于用户 TenantId 时，
    /// 表示该用户由其他租户（通常是平台租户）创建，本租户管理员不可见。
    /// </summary>
    public long CreatorTenantId { get; set; }

    /// <summary>
    /// 导航属性：用户角色关联
    /// </summary>
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    /// <summary>
    /// 导航属性：组织
    /// </summary>
    public virtual Organization? Organization { get; set; }
}