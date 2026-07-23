using Bms.System.Domain.Entities;

namespace Bms.System.Domain.Interfaces;

/// <summary>
/// 数据权限过滤接口
/// </summary>
public interface IDataPermissionFilter
{
    /// <summary>
    /// 获取用户可访问的组织ID列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>可访问的组织ID列表</returns>
    Task<List<long>> GetAccessibleOrganizationIdsAsync(long userId);

    /// <summary>
    /// 检查用户是否有数据访问权限
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="targetUserId">目标数据的用户ID</param>
    /// <param name="targetOrganizationId">目标数据的组织ID</param>
    /// <param name="allowSelf">是否允许操作自己。Update/AssignRoles 等场景传 false，避免"自己永远有权限"绕过禁改自己规则</param>
    /// <returns>是否有权限</returns>
    Task<bool> HasDataPermissionAsync(long userId, long? targetUserId = null, long? targetOrganizationId = null, bool allowSelf = true);

    /// <summary>
    /// 获取用户的数据权限范围
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>数据权限范围</returns>
    Task<DataPermissionScope> GetDataPermissionScopeAsync(long userId);
}

/// <summary>
/// 数据权限范围
/// </summary>
public class DataPermissionScope
{
    /// <summary>
    /// 数据范围类型
    /// </summary>
    public Enums.DataScopeType ScopeType { get; set; }

    /// <summary>
    /// 可访问的组织ID列表
    /// </summary>
    public List<long> OrganizationIds { get; set; } = new List<long>();

    /// <summary>
    /// 用户ID
    /// </summary>
    public long UserId { get; set; }
}
