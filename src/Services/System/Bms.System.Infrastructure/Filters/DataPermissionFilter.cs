using Bms.System.Domain.Entities;
using Bms.System.Domain.Enums;
using Bms.System.Domain.Interfaces;
using Bms.System.Domain.IRepositories;

namespace Bms.System.Infrastructure.Filters;

/// <summary>
/// 数据权限过滤器实现
/// </summary>
public class DataPermissionFilter : IDataPermissionFilter
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IDataPermissionRepository _dataPermissionRepository;
    private readonly IOrganizationRepository _organizationRepository;

    public DataPermissionFilter(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IDataPermissionRepository dataPermissionRepository,
        IOrganizationRepository organizationRepository)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _dataPermissionRepository = dataPermissionRepository;
        _organizationRepository = organizationRepository;
    }

    public async Task<List<long>> GetAccessibleOrganizationIdsAsync(long userId)
    {
        var scope = await GetDataPermissionScopeAsync(userId);
        return scope.OrganizationIds;
    }

    public async Task<bool> HasDataPermissionAsync(long userId, long? targetUserId = null, long? targetOrganizationId = null, bool allowSelf = true)
    {
        var scope = await GetDataPermissionScopeAsync(userId);

        switch (scope.ScopeType)
        {
            case DataScopeType.All:
                return true;

            case DataScopeType.Self:
                return allowSelf && targetUserId.HasValue && targetUserId.Value == userId;

            case DataScopeType.DepartmentAndBelow:
            case DataScopeType.Custom:
                // 禁改自己场景：allowSelf=false 时不再放行 targetUserId==userId
                if (allowSelf && targetUserId.HasValue && targetUserId.Value == userId)
                {
                    return true;
                }
                if (targetOrganizationId.HasValue)
                {
                    return scope.OrganizationIds.Contains(targetOrganizationId.Value);
                }
                return false;

            default:
                return false;
        }
    }

    public async Task<DataPermissionScope> GetDataPermissionScopeAsync(long userId)
    {
        var result = new DataPermissionScope
        {
            UserId = userId,
            ScopeType = DataScopeType.Self,
            OrganizationIds = new List<long>()
        };

        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            return result;
        }

        // 获取用户的所有角色
        var roles = await _roleRepository.GetByUserIdAsync(userId);
        if (!roles.Any())
        {
            return result;
        }

        // 获取所有角色的数据权限
        // 权限合并逻辑：取最宽权限（All > DepartmentAndBelow/Custom > Self）
        // 多角色场景下，用户拥有任一角色的最高权限即按该权限处理
        bool hasSelf = false;
        bool hasAll = false;
        bool hasDepartmentOrCustom = false;
        var allOrgIds = new List<long>();

        foreach (var role in roles)
        {
            var dataPermission = await _dataPermissionRepository.GetByRoleIdAsync(role.Id);
            if (dataPermission == null)
            {
                // 角色没有 DataPermission 记录，视为 Self（默认拒绝）
                hasSelf = true;
                continue;
            }

            var scopeType = (DataScopeType)dataPermission.DataScopeType;

            switch (scopeType)
            {
                case DataScopeType.Self:
                    hasSelf = true;
                    break;

                case DataScopeType.All:
                    hasAll = true;
                    // All 类型也填充组织列表，避免写入校验时 OrganizationIds 为空导致校验失效
                    if (user.TenantId > 0)
                    {
                        var tenantOrgs = await _organizationRepository.GetByTenantIdAsync(user.TenantId);
                        foreach (var orgId in tenantOrgs.Select(o => o.Id))
                        {
                            if (!allOrgIds.Contains(orgId))
                            {
                                allOrgIds.Add(orgId);
                            }
                        }
                    }
                    break;

                case DataScopeType.DepartmentAndBelow:
                    hasDepartmentOrCustom = true;
                    // 添加用户所在部门
                    if (user.OrganizationId.HasValue && !allOrgIds.Contains(user.OrganizationId.Value))
                    {
                        allOrgIds.Add(user.OrganizationId.Value);
                        // 获取所有下级组织
                        var childOrgIds = await GetAllChildOrganizationIdsAsync(user.OrganizationId.Value);
                        allOrgIds.AddRange(childOrgIds);
                    }
                    break;

                case DataScopeType.Custom:
                    hasDepartmentOrCustom = true;
                    // 添加自定义组织ID
                    if (!string.IsNullOrEmpty(dataPermission.CustomOrganizationIds))
                    {
                        var orgIds = dataPermission.CustomOrganizationIds
                            .Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(long.Parse)
                            .ToList();
                        allOrgIds.AddRange(orgIds);
                    }
                    break;
            }
        }

        // 权限级别判断：取最宽权限 All > DepartmentAndBelow/Custom > Self
        if (hasAll)
        {
            result.ScopeType = DataScopeType.All;
            result.OrganizationIds = allOrgIds.Distinct().ToList();
        }
        else if (hasDepartmentOrCustom)
        {
            result.ScopeType = DataScopeType.DepartmentAndBelow;
            result.OrganizationIds = allOrgIds.Distinct().ToList();
        }
        else if (hasSelf)
        {
            result.ScopeType = DataScopeType.Self;
        }
        else
        {
            // 默认视为 Self（默认拒绝）
            result.ScopeType = DataScopeType.Self;
        }

        return result;
    }

    /// <summary>
    /// 递归获取指定组织的所有下级组织ID
    /// </summary>
    private async Task<List<long>> GetAllChildOrganizationIdsAsync(long organizationId)
    {
        var result = new List<long>();
        var children = await _organizationRepository.GetChildrenAsync(organizationId);

        foreach (var child in children)
        {
            result.Add(child.Id);
            // 递归获取子级的子级
            var childIds = await GetAllChildOrganizationIdsAsync(child.Id);
            result.AddRange(childIds);
        }

        return result;
    }
}
