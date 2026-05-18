namespace Bms.System.Application.Dtos.Users;

/// <summary>
/// 用户创建上下文，包含当前用户信息用于权限判断
/// </summary>
public class UserCreateContext
{
    /// <summary>
    /// 当前用户角色列表
    /// </summary>
    public List<string> CurrentUserRoles { get; set; } = new();

    /// <summary>
    /// 当前用户的租户ID
    /// </summary>
    public long CurrentTenantId { get; set; }

    /// <summary>
    /// 当前用户的租户编码
    /// </summary>
    public string CurrentTenantCode { get; set; } = string.Empty;

    /// <summary>
    /// 判断当前用户是否为超级管理员
    /// </summary>
    public bool IsSuperAdmin => CurrentUserRoles.Contains("super_admin");

    /// <summary>
    /// 判断当前用户是否为管理员
    /// </summary>
    public bool IsTenantAdmin => CurrentUserRoles.Contains("tenant_admin");
}