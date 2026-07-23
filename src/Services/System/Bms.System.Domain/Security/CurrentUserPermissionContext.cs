using Bms.System.Domain.Interfaces;

namespace Bms.System.Domain.Security;

/// <summary>
/// 当前用户权限上下文
/// 封装校验所需的关键信息，避免每个校验方法重复查询
/// </summary>
public class CurrentUserPermissionContext
{
    /// <summary>
    /// 当前用户ID
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 是否超级管理员（角色 Code 含 super_admin）
    /// </summary>
    public bool IsSuperAdmin { get; set; }

    /// <summary>
    /// 是否租户管理员（角色 Code 含 tenant_admin）
    /// </summary>
    public bool IsTenantAdmin { get; set; }

    /// <summary>
    /// 当前用户的租户ID
    /// </summary>
    public long TenantId { get; set; }

    /// <summary>
    /// 当前用户最高角色等级（数字最小）
    /// super_admin=0, tenant_admin=1, 普通角色 > 1
    /// </summary>
    public int MaxRoleLevel { get; set; }

    /// <summary>
    /// 当前用户的数据权限范围
    /// </summary>
    public DataPermissionScope DataScope { get; set; } = null!;
}
