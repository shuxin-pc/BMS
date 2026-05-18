using Bms.BuildingBlocks.MultiTenant.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Bms.BuildingBlocks.MultiTenant.Middleware;

/// <summary>
/// 多租户中间件
/// </summary>
public class MultiTenantMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<MultiTenantMiddleware> _logger;

    public MultiTenantMiddleware(
        RequestDelegate next,
        ILogger<MultiTenantMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ITenantProvider tenantProvider)
    {
        try
        {
            // 跳过认证接口的租户检查
            var path = context.Request.Path.Value ?? string.Empty;
            if (path.StartsWith("/api/internal/auth", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/api/auth", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/api/system/auth", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/api/system/SystemConfigs/system", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogDebug("Skipping tenant check for auth endpoint: {Url}", context.Request.GetDisplayUrl());
                await _next(context);
                return;
            }

            // 调试：输出所有 Claims
            _logger.LogInformation("=== Tenant Debug: All Claims ===");
            foreach (var claim in context.User.Claims)
            {
                _logger.LogInformation("Claim: {Type} = {Value}", claim.Type, claim.Value);
            }

            // 获取用户名（从Claims中获取），尝试多种可能的 claim 类型
            var userNameClaim = context.User.FindFirst("name")?.Value
                              ?? context.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name")?.Value
                              ?? context.User.FindFirst("preferred_username")?.Value;

            _logger.LogInformation("=== Tenant Debug: UserName claim = {UserName} ===", userNameClaim);

            if (userNameClaim == "admin")
            {
                _logger.LogInformation("Skipping tenant check for system admin user: {UserName}", userNameClaim);
                await _next(context);
                return;
            }

            var tenant = await tenantProvider.GetCurrentTenantAsync(context.RequestAborted);
            _logger.LogInformation("=== Tenant Debug: Tenant resolved: {TenantId}, {TenantName} ===",
                tenant?.Id, tenant?.Name);

            if (tenant == null)
            {
                _logger.LogWarning("出现错误：找不到租户信息，请求：{Url}", context.Request.GetDisplayUrl());
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await WriteErrorResponse(context, 400, "无效的租户信息");
                return;
            }

            if (!tenant.IsEnabled)
            {
                _logger.LogWarning("出现错误：租户已禁用，租户ID：{TenantId}，租户名称：{TenantName}", tenant.Id, tenant.Name);
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await WriteErrorResponse(context, 403, "租户已被禁用");
                return;
            }

            if (tenant.ExpireTime.HasValue && tenant.ExpireTime < DateTime.UtcNow)
            {
                _logger.LogWarning("出现错误：租户已过期，租户ID：{TenantId}，过期时间：{ExpireTime}", tenant.Id, tenant.ExpireTime);
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await WriteErrorResponse(context, 403, "租户已过期");
                return;
            }

            // 将租户信息存入HttpContext
            context.Items["TenantInfo"] = tenant;

            // 将租户ID添加到响应头，方便调试
            context.Response.Headers["X-Tenant-Id"] = tenant.Id.ToString();
            context.Response.Headers["X-Tenant-Code"] = tenant.Code;

            _logger.LogDebug("已解析租户：租户ID={TenantId}，租户名称={TenantName}，请求：{Url}",
                tenant.Id, tenant.Name, context.Request.GetDisplayUrl());

            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "出现错误：解析租户时发生异常，请求：{Url}", context.Request.GetDisplayUrl());
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await WriteErrorResponse(context, 500, "服务器内部错误");
        }
    }

    private static async Task WriteErrorResponse(HttpContext context, int code, string message)
    {
        context.Response.ContentType = "application/json";
        var response = new
        {
            code,
            message,
            requestId = context.TraceIdentifier,
            timestamp = DateTime.UtcNow
        };
        await JsonSerializer.SerializeAsync(context.Response.Body, response);
    }
}
