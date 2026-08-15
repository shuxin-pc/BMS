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
            RoleIds = roles.Select(r => r.Id).ToList(),
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

        // 非超级管理员（含 tenant_admin 与普通用户）统一校验：
        // ① 目标角色必须属于本租户（tenant_admin 角色属于平台租户，仅 super_admin 可分配）
        // ② 受保护角色（super_admin/tenant_admin）仅 super_admin 可分配
        // ③ 只能分配严格低于自己等级的角色（Level 数字越大权限越低）
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

        // 显式允许操作自己时，跳过后续级别/组织范围校验
        // 自身对自身的级别比较无意义（必然相等），否则会误报"无权操作同级或更高级别的用户"
        if (allowSelf && targetUserId == ctx.UserId)
        {
            return;
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

        // 非超级管理员（含 tenant_admin 与普通用户）统一校验：
        // - 不能操作 super_admin / tenant_admin 账号（受保护角色，仅 super_admin 可操作）
        // - 只能操作严格高于自己等级的用户（Level 数字越大权限越低）
        // - 目标用户当前组织必须在数据权限范围内
        // 注：tenant_admin 之间 Level 相同（均为 1），自然被"严格高于自己等级"规则拒绝，
        //     无需专门的同级越权校验（H4 校验已移除）
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
    public async Task CheckCanMoveToOrganizationAsync(CurrentUserPermissionContext ctx, long? targetOrganizationId, bool isCreate = false)
    {
        if (!targetOrganizationId.HasValue)
        {
            // null 语义按场景区分：
            // - super_admin：创建/编辑均放行（平台管理员等无组织用户）
            // - 非超管 + 创建场景：组织必填，提示「创建用户时必须指定组织归属」
            // - 非超管 + 编辑场景：禁止清空已有组织，提示「无权清空用户的组织归属」（原逻辑）
            if (ctx.IsSuperAdmin)
            {
                return;
            }
            throw new PermissionDeniedException(isCreate
                ? "创建用户时必须指定组织归属"
                : "无权清空用户的组织归属");
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

        // 非超级管理员（含 tenant_admin 与普通用户）：目标组织必须在数据权限范围内
        // tenant_admin 的 DataScope=All，OrganizationIds 自然包含本租户全量组织，
        // 跨租户组织不在范围内会被拒绝（等价于租户隔离校验）
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
