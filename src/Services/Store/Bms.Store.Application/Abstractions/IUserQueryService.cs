namespace Bms.Store.Application.Abstractions;

/// <summary>
/// 跨服务用户查询抽象
/// Store 服务不持有 System 服务的 DbContext，但需要展示租户内用户列表用于门店授权分配。
/// 与 UserStatusCheckMiddleware 一致，通过 Npgsql 直查 bms_system."Users" 表。
/// </summary>
public interface IUserQueryService
{
    /// <summary>
    /// 获取指定租户下处于启用状态（Status=1）的有效用户列表，用于门店授权分配弹窗。
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <returns>租户内有效用户列表（按 Id 升序）</returns>
    Task<List<TenantUserDto>> GetActiveUsersByTenantAsync(long tenantId);

    /// <summary>
    /// 获取指定租户下拥有 tenant_admin 角色的有效用户列表。
    /// 用于门店创建时自动为租户管理员分配门店访问权限（UserStore 记录）。
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <returns>租户内 tenant_admin 用户列表（按 Id 升序）</returns>
    Task<List<TenantUserDto>> GetTenantAdminUsersAsync(long tenantId);
}

/// <summary>
/// 租户用户简略信息（用于门店授权分配场景，仅暴露必要字段）
/// </summary>
public class TenantUserDto
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 登录用户名
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// 真实姓名
    /// </summary>
    public string RealName { get; set; } = string.Empty;
}

/// <summary>
/// 可分配用户信息（在租户有效用户基础上标记是否已分配给目标门店）
/// </summary>
public class AvailableUserDto : TenantUserDto
{
    /// <summary>
    /// 是否已分配给当前门店
    /// </summary>
    public bool Assigned { get; set; }
}
