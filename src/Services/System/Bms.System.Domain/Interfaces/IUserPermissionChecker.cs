using Bms.System.Domain.Security;

namespace Bms.System.Domain.Interfaces;

/// <summary>
/// 用户权限校验器
/// 统一封装用户管理写操作的权限校验规则，避免 6 个接口重复实现
/// 校验规则参考《用户管理权限提升修复方案》3.4 节
/// </summary>
public interface IUserPermissionChecker
{
    /// <summary>
    /// 获取当前用户权限上下文（包含身份、租户、角色等级、数据范围）
    /// </summary>
    Task<CurrentUserPermissionContext> GetContextAsync(long currentUserId);

    /// <summary>
    /// 校验能否分配指定角色（规则 1 + 受保护角色）
    /// - super_admin：放行
    /// - tenant_admin：待分配角色 Code 不能是 super_admin/tenant_admin，且必须属于本租户
    /// - 普通用户：待分配角色必须属于本租户，Code 不能是 super_admin/tenant_admin，且 Level 严格高于自己
    /// </summary>
    Task CheckCanAssignRolesAsync(CurrentUserPermissionContext ctx, List<long> targetRoleIds);

    /// <summary>
    /// 校验能否操作目标用户（规则 2 + 组织范围）
    /// - super_admin：放行
    /// - tenant_admin：目标用户必须属于本租户，且不能是 super_admin
    /// - 普通用户：目标用户必须属于本租户，不能是 super_admin/tenant_admin，MaxRoleLevel 严格高于自己，且组织在 DataScope 内
    /// - allowSelf=false 时禁止操作自己
    /// </summary>
    Task CheckCanOperateUserAsync(CurrentUserPermissionContext ctx, long targetUserId, bool allowSelf = false);

    /// <summary>
    /// 校验能否把用户移动到指定组织（规则 3）
    /// - super_admin：放行
    /// - tenant_admin：目标组织必须属于本租户
    /// - 普通用户：目标组织必须在 DataScope.OrganizationIds 内
    /// </summary>
    Task CheckCanMoveToOrganizationAsync(CurrentUserPermissionContext ctx, long? targetOrganizationId);

    /// <summary>
    /// 校验能否修改目标用户租户归属（规则 4）
    /// 仅 super_admin 可改租户字段
    /// </summary>
    Task CheckCanChangeTenantAsync(CurrentUserPermissionContext ctx);
}
