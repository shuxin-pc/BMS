using System.Reflection;
using System.Security.Claims;
using Bms.BuildingBlocks.Abstractions.Security;
using Bms.System.Domain.IRepositories;

namespace Bms.System.Api.Middleware;

/// <summary>
/// 权限校验中间件
/// </summary>
public class PermissionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PermissionMiddleware> _logger;

    public PermissionMiddleware(RequestDelegate next, ILogger<PermissionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IUserRepository userRepository, IRoleRepository roleRepository, IRoleMenuAuthRepository roleMenuAuthRepository)
    {
        // 获取当前用户ID（从Claims中获取）
        // 兼容 "sub"（OpenIddict 原始 claim）和 ClaimTypes.NameIdentifier（JWT 默认映射后的 claim）
        // 避免因 JwtBearer 自动映射导致 FindFirst("sub") 返回 null 而跳过权限校验
        var userIdClaim = context.User.FindFirst("sub")?.Value
            ?? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
        {
            await _next(context);
            return;
        }

        // 获取Endpoint信息，检查是否有PermissionAttribute
        var endpoint = context.GetEndpoint();
        if (endpoint == null)
        {
            await _next(context);
            return;
        }

        var permissionAttributes = endpoint.Metadata.GetOrderedMetadata<PermissionAttribute>();
        if (!permissionAttributes.Any())
        {
            // 没有权限要求，直接通过
            await _next(context);
            return;
        }

        // 检查用户权限
        var hasPermission = await CheckUserPermissionAsync(roleRepository, roleMenuAuthRepository, userId, permissionAttributes);
        if (!hasPermission)
        {
            // 返回路径和所需权限码，便于前端定位是哪个接口、缺什么权限
            var requiredPermissions = permissionAttributes.Select(a => a.PermissionCode).ToList();
            _logger.LogWarning(
                "[PermissionDenied] Path={Path}, UserId={UserId}, RequiredPermissions={Required}",
                context.Request.Path.Value, userId, string.Join(",", requiredPermissions));
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new
            {
                Code = 403,
                Message = "没有权限访问该资源",
                Path = context.Request.Path.Value,
                RequiredPermissions = requiredPermissions
            });
            return;
        }

        await _next(context);
    }

    private async Task<bool> CheckUserPermissionAsync(
        IRoleRepository roleRepository,
        IRoleMenuAuthRepository roleMenuAuthRepository,
        long userId,
        IEnumerable<PermissionAttribute> permissionAttributes)
    {
        // 获取用户的角色
        var roles = await roleRepository.GetByUserIdAsync(userId);
        var roleCodes = roles.Select(r => r.Code).ToList();
        if (!roles.Any())
        {
            _logger.LogWarning("【权限调试】UserId={UserId} 没有任何角色", userId);
            return false;
        }

        // super_admin 自动放行所有权限检查
        // super_admin 是平台级管理员，通过角色 Code 判断（不依赖菜单授权）
        // tenant_admin 不自动放行：菜单权限由 super_admin 通过 UI 配置（RoleMenuAuth 表），无种子数据
        if (roles.Any(r => string.Equals(r.Code, "super_admin", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        // 系统通过菜单授权（RoleMenuAuths）管理权限，权限码存储在 Menu.PermissionCode 字段
        // 从 RoleMenuAuths -> Menu -> PermissionCode 获取用户的所有权限码
        var roleIds = roles.Select(r => r.Id).ToList();
        var roleMenuAuths = await roleMenuAuthRepository.GetByRoleIdsAsync(roleIds);
        var allPermissions = roleMenuAuths
            .Select(rma => rma.Menu?.PermissionCode)
            .Where(c => !string.IsNullOrEmpty(c))
            .Select(c => c!)
            .ToList();

        var requiredCodes = permissionAttributes.Select(a => a.PermissionCode).ToList();
        _logger.LogWarning(
            "【权限调试】UserId={UserId}, Roles=[{Roles}], UserPermissions(from RoleMenuAuths)=[{Perms}], Required=[{Required}]",
            userId, string.Join(",", roleCodes), string.Join(",", allPermissions), string.Join(",", requiredCodes));

        // 检查是否满足任一权限要求（OR逻辑）
        foreach (var attr in permissionAttributes)
        {
            if (allPermissions.Contains(attr.PermissionCode, StringComparer.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}

/// <summary>
/// 权限中间件扩展方法
/// </summary>
public static class PermissionMiddlewareExtensions
{
    /// <summary>
    /// 使用权限校验中间件
    /// </summary>
    public static IApplicationBuilder UsePermissionMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<PermissionMiddleware>();
    }
}
