using Bms.System.Domain.Entities;
using Bms.System.Domain.Exceptions;
using Bms.System.Domain.Interfaces;
using Bms.System.Domain.IRepositories;
using Bms.System.Domain.Security;

namespace Bms.System.Infrastructure.Services;

/// <summary>
/// 用户权限校验器实现
/// 校验规则参考《用户管理权限提升修复方案》3.4 节
/// 三级权限模型：super_admin > tenant_admin > 普通用户
/// </summary>
public class UserPermissionChecker : IUserPermissionChecker
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IDataPermissionFilter _dataPermissionFilter;

    // 受保护角色 Code：仅 super_admin 可分配/操作
    private static readonly HashSet<string> ProtectedRoleCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "super_admin",
        "tenant_admin"
    };

    public UserPermissionChecker(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IOrganizationRepository organizationRepository,
        IDataPermissionFilter dataPermissionFilter)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _organizationRepository = organizationRepository;
        _dataPermissionFilter = dataPermissionFilter;
    }

    /// <inheritdoc />
    public async Task<CurrentUserPermissionContext> GetContextAsync(long currentUserId)
    {
        var user = await _userRepository.GetByIdAsync(currentUserId)
            ?? throw new PermissionDeniedException("当前用户不存在");

        var roles = await _roleRepository.GetByUserIdAsync(currentUserId);

        var isSuperAdmin = roles.Any(r => string.Equals(r.Code, "super_admin", StringComparison.OrdinalIgnoreCase));
        var isTenantAdmin = roles.Any(r => string.Equals(r.Code, "tenant_admin", StringComparison.OrdinalIgnoreCase));

        // MaxRoleLevel：数字越小权限越大。无角色时视为 100（普通角色默认值）
        var maxRoleLevel = roles.Any() ? roles.Min(r => r.Level) : 100;

        var dataScope = await _dataPermissionFilter.GetDataPermissionScopeAsync(currentUserId);

        return new CurrentUserPermissionContext
        {
            UserId = currentUserId,
            IsSuperAdmin = isSuperAdmin,
            IsTenantAdmin = isTenantAdmin,
            TenantId = user.TenantId,
            MaxRoleLevel = maxRoleLevel,
            DataScope = dataScope
        };
    }

    /// <inheritdoc />
    public async Task CheckCanAssignRolesAsync(CurrentUserPermissionContext ctx, List<long> targetRoleIds)
    {
        if (ctx.IsSuperAdmin)
        {
            return;
        }

        if (!targetRoleIds.Any())
        {
            return;
        }

        // 加载待分配角色
        var targetRoles = new List<Role>();
        foreach (var roleId in targetRoleIds)
        {
            var role = await _roleRepository.GetByIdAsync(roleId);
            if (role == null)
            {
                throw new PermissionDeniedException($"角色 {roleId} 不存在");
            }
            targetRoles.Add(role);
        }

        if (ctx.IsTenantAdmin)
        {
            foreach (var role in targetRoles)
            {
                if (ProtectedRoleCodes.Contains(role.Code))
                {
                    throw new PermissionDeniedException("无权分配系统保留角色");
                }
                if (role.TenantId != ctx.TenantId)
                {
                    throw new PermissionDeniedException("无权分配其他租户的角色");
                }
            }
            return;
        }

        // 普通用户
        foreach (var role in targetRoles)
        {
            if (role.TenantId != ctx.TenantId)
            {
                throw new PermissionDeniedException("无权分配其他租户的角色");
            }
            if (ProtectedRoleCodes.Contains(role.Code))
            {
                throw new PermissionDeniedException("无权分配系统保留角色");
            }
            // 只能分配严格低于自己等级的角色
            if (role.Level <= ctx.MaxRoleLevel)
            {
                throw new PermissionDeniedException($"无权分配与自身同级或更高级别的角色（{role.Name}）");
            }
        }
    }

    /// <inheritdoc />
    public async Task CheckCanOperateUserAsync(CurrentUserPermissionContext ctx, long targetUserId, bool allowSelf = false)
    {
        // 禁改自己（除非显式允许，如修改自己的非敏感信息）
        if (!allowSelf && targetUserId == ctx.UserId)
        {
            throw new PermissionDeniedException("无权操作自己的账号");
        }

        if (ctx.IsSuperAdmin)
        {
            return;
        }

        var targetUser = await _userRepository.GetByIdAsync(targetUserId)
            ?? throw new PermissionDeniedException("目标用户不存在");

        if (targetUser.TenantId != ctx.TenantId)
        {
            throw new PermissionDeniedException("无权操作其他租户的用户");
        }

        var targetRoles = await _roleRepository.GetByUserIdAsync(targetUserId);
        var targetIsSuperAdmin = targetRoles.Any(r => string.Equals(r.Code, "super_admin", StringComparison.OrdinalIgnoreCase));
        var targetIsTenantAdmin = targetRoles.Any(r => string.Equals(r.Code, "tenant_admin", StringComparison.OrdinalIgnoreCase));

        if (ctx.IsTenantAdmin)
        {
            // tenant_admin 不能操作 super_admin
            if (targetIsSuperAdmin)
            {
                throw new PermissionDeniedException("无权操作超级管理员账号");
            }
            return;
        }

        // 普通用户
        if (targetIsSuperAdmin || targetIsTenantAdmin)
        {
            throw new PermissionDeniedException("无权操作管理员账号");
        }

        var targetMaxLevel = targetRoles.Any() ? targetRoles.Min(r => r.Level) : 100;
        // 只能操作严格高于自己等级的用户（数字更大）
        if (targetMaxLevel <= ctx.MaxRoleLevel)
        {
            throw new PermissionDeniedException("无权操作同级或更高级别的用户");
        }

        // 组织范围校验：目标用户当前组织必须在数据权限范围内
        // 普通用户场景：目标用户无组织时无法验证其是否在数据权限范围内，默认拒绝（纵深防御）
        if (!targetUser.OrganizationId.HasValue)
        {
            throw new PermissionDeniedException("目标用户未分配组织，无权操作");
        }
        if (!ctx.DataScope.OrganizationIds.Contains(targetUser.OrganizationId.Value))
        {
            throw new PermissionDeniedException("目标用户不在你的数据权限范围内");
        }
    }

    /// <inheritdoc />
    public async Task CheckCanMoveToOrganizationAsync(CurrentUserPermissionContext ctx, long? targetOrganizationId)
    {
        if (!targetOrganizationId.HasValue)
        {
            // 清空组织：super_admin/tenant_admin 放行；普通用户不允许（避免逃避组织约束）
            if (!ctx.IsSuperAdmin && !ctx.IsTenantAdmin)
            {
                throw new PermissionDeniedException("无权清空用户的组织归属");
            }
            return;
        }

        if (ctx.IsSuperAdmin)
        {
            return;
        }

        var targetOrg = await _organizationRepository.GetByIdAsync(targetOrganizationId.Value);
        if (targetOrg == null)
        {
            throw new PermissionDeniedException("目标组织不存在");
        }

        if (ctx.IsTenantAdmin)
        {
            if (targetOrg.TenantId != ctx.TenantId)
            {
                throw new PermissionDeniedException("无权将用户移动到其他租户的组织");
            }
            return;
        }

        // 普通用户
        if (!ctx.DataScope.OrganizationIds.Contains(targetOrganizationId.Value))
        {
            throw new PermissionDeniedException("目标组织不在你的数据权限范围内");
        }
    }

    /// <inheritdoc />
    public Task CheckCanChangeTenantAsync(CurrentUserPermissionContext ctx)
    {
        if (!ctx.IsSuperAdmin)
        {
            throw new PermissionDeniedException("无权修改用户的租户归属，仅超级管理员可操作");
        }
        return Task.CompletedTask;
    }
}
