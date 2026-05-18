using Bms.BuildingBlocks.MultiTenant.Abstractions;
using Bms.BuildingBlocks.MultiTenant.Models;

namespace Bms.Gateway.Api.Middleware;

/// <summary>
/// 子系统访问权限验证中间件
/// </summary>
public class SubsystemAccessMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SubsystemAccessMiddleware> _logger;

    public SubsystemAccessMiddleware(RequestDelegate next, ILogger<SubsystemAccessMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ITenantProvider? tenantProvider)
    {
        // 如果没有租户提供者，跳过验证
        if (tenantProvider == null)
        {
            await _next(context);
            return;
        }

        // 获取当前租户
        var tenant = await tenantProvider.GetCurrentTenantAsync();
        if (tenant == null)
        {
            // 无租户信息，继续处理（可能是不需要租户的路径）
            await _next(context);
            return;
        }

        // 获取请求的子系统标识
        var subsystemCode = GetSubsystemCode(context.Request.Path);
        if (string.IsNullOrEmpty(subsystemCode))
        {
            await _next(context);
            return;
        }

        // 检查子系统访问权限
        if (!tenant.CanAccessSubsystem(subsystemCode))
        {
            _logger.LogWarning("租户 {TenantCode} 尝试访问未授权的子系统 {SubsystemCode}",
                tenant.Code, subsystemCode);

            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new
            {
                code = 403,
                message = $"无权访问子系统：{subsystemCode}",
                data = (object?)null
            });
            return;
        }

        await _next(context);
    }

    /// <summary>
    /// 从请求路径中提取子系统标识
    /// 例如：/api/system/users -> bms-system
    ///       /api/production/orders -> bms-production
    /// </summary>
    private static string? GetSubsystemCode(PathString path)
    {
        if (string.IsNullOrEmpty(path))
            return null;

        var pathSegments = path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (pathSegments == null || pathSegments.Length < 2)
            return null;

        // /api/{subsystem}/... -> 第二个segment是子系统标识
        if (pathSegments[0].Equals("api", StringComparison.OrdinalIgnoreCase))
        {
            var subsystem = pathSegments[1].ToLowerInvariant();
            // 映射URL路径到子系统Code
            return subsystem switch
            {
                "system" => "bms-system",
                "identity" => "bms-identity",
                "production" => "bms-production",
                "quality" => "bms-quality",
                "warehouse" => "bms-warehouse",
                "equipment" => "bms-equipment",
                _ => subsystem
            };
        }

        return null;
    }
}

/// <summary>
/// 中间件扩展方法
/// </summary>
public static class SubsystemAccessMiddlewareExtensions
{
    public static IApplicationBuilder UseSubsystemAccess(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SubsystemAccessMiddleware>();
    }
}