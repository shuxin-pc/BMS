using System.Reflection;
using Bms.System.Domain.Attributes;
using Bms.System.Domain.IRepositories;

namespace Bms.System.Api.Middleware;

/// <summary>
/// 权限校验中间件
/// </summary>
public class PermissionMiddleware
{
    private readonly RequestDelegate _next;

    public PermissionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IUserRepository userRepository, IRoleRepository roleRepository)
    {
        // 获取当前用户ID（从Claims中获取）
        var userIdClaim = context.User.FindFirst("sub")?.Value;
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
        var hasPermission = await CheckUserPermissionAsync(userRepository, roleRepository, userId, permissionAttributes);
        if (!hasPermission)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { Code = 403, Message = "没有权限访问该资源" });
            return;
        }

        await _next(context);
    }

    private async Task<bool> CheckUserPermissionAsync(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        long userId,
        IEnumerable<PermissionAttribute> permissionAttributes)
    {
        // 获取用户的角色
        var roles = await roleRepository.GetByUserIdAsync(userId);
        if (!roles.Any())
        {
            return false;
        }

        // 获取用户的所有权限
        var allPermissions = new List<string>();
        foreach (var role in roles)
        {
            var rolePermissions = role.RolePermissions.Select(rp => rp.Permission?.Code).Where(c => !string.IsNullOrEmpty(c)).ToList();
            allPermissions.AddRange(rolePermissions);
        }

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
