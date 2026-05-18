namespace Bms.System.Domain.Entities;

/// <summary>
/// 用户角色关联实体
/// </summary>
public class UserRole : EntityBase
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 角色ID
    /// </summary>
    public long RoleId { get; set; }

    /// <summary>
    /// 导航属性：用户
    /// </summary>
    public virtual User? User { get; set; }

    /// <summary>
    /// 导航属性：角色
    /// </summary>
    public virtual Role? Role { get; set; }
}