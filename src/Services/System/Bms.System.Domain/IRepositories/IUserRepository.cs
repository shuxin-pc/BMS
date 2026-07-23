using Bms.System.Domain.Entities;

namespace Bms.System.Domain.IRepositories;

/// <summary>
/// 用户仓储接口
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// 根据ID获取用户
    /// </summary>
    Task<User?> GetByIdAsync(long id);

    /// <summary>
    /// 根据用户名获取用户
    /// </summary>
    Task<User?> GetByUserNameAsync(string userName);

    /// <summary>
    /// 根据手机号获取用户
    /// </summary>
    Task<User?> GetByPhoneAsync(string phone);

    /// <summary>
    /// 根据邮箱获取用户
    /// </summary>
    Task<User?> GetByEmailAsync(string email);

    /// <summary>
    /// 获取所有用户列表
    /// </summary>
    /// <param name="tenantId">租户ID筛选</param>
    Task<List<User>> GetListAsync(long? tenantId = null);

    /// <summary>
    /// 分页查询用户列表
    /// </summary>
    /// <param name="pageIndex">页码</param>
    /// <param name="pageSize">每页数量</param>
    /// <param name="userName">用户名（模糊搜索）</param>
    /// <param name="realName">真实姓名（模糊搜索）</param>
    /// <param name="status">状态筛选</param>
    /// <param name="tenantId">租户ID筛选</param>
    /// <param name="organizationId">组织ID筛选</param>
    /// <param name="userId">用户ID筛选（仅本人模式）</param>
    /// <param name="organizationIds">组织ID列表筛选（部门及以下/自定义模式）</param>
    /// <param name="roleId">角色ID筛选</param>
    /// <param name="creatorTenantId">创建者租户ID筛选（屏蔽平台跨租户创建的用户）</param>
    Task<List<User>> GetPagedListAsync(int pageIndex, int pageSize, string? userName = null, string? realName = null, int? status = null, long? tenantId = null, long? organizationId = null, long? userId = null, List<long>? organizationIds = null, long? roleId = null, long? creatorTenantId = null);

    /// <summary>
    /// 获取分页查询的总数量
    /// </summary>
    Task<int> GetCountAsync(string? userName = null, string? realName = null, int? status = null, long? tenantId = null, long? organizationId = null, long? userId = null, List<long>? organizationIds = null, long? roleId = null, long? creatorTenantId = null);

    /// <summary>
    /// 添加用户
    /// </summary>
    Task<User> AddAsync(User user);

    /// <summary>
    /// 更新用户
    /// </summary>
    Task UpdateAsync(User user);

    /// <summary>
    /// 删除用户（软删除）
    /// </summary>
    Task DeleteAsync(long id);

    /// <summary>
    /// 检查用户名是否存在
    /// </summary>
    Task<bool> ExistsUserNameAsync(string userName, long? excludeId = null);

    /// <summary>
    /// 检查手机号是否存在
    /// </summary>
    Task<bool> ExistsPhoneAsync(string phone, long? excludeId = null);

    /// <summary>
    /// 检查邮箱是否存在
    /// </summary>
    Task<bool> ExistsEmailAsync(string email, long? excludeId = null);

    /// <summary>
    /// 获取用户角色列表
    /// </summary>
    Task<List<Role>> GetUserRolesAsync(long userId);

    /// <summary>
    /// 获取用户权限列表
    /// </summary>
    Task<List<Permission>> GetUserPermissionsAsync(long userId);
}