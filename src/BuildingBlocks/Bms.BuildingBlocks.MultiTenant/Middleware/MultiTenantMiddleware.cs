using Bms.BuildingBlocks.MultiTenant.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
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
            // 跳过非 API 路径的租户检查（如 Swagger、根路径、favicon 等）
            var path = context.Request.Path.Value ?? string.Empty;
            var method = context.Request.Method;
            _logger.LogInformation("调试：MultiTenant 收到请求 {Method} {Path}", method, path);

            if (!path.StartsWith("/api", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogDebug("Skipping tenant check for non-API path: {Url}", context.Request.GetDisplayUrl());
                await _next(context);
                return;
            }

            // 跳过认证接口的租户检查
            // 注意：/api/internal/* 路径不再豁免，内部服务由 InternalServiceAuthMiddleware
            // 设置 tenant_id claim，ClaimTenantResolver 可正常解析租户上下文
            if (path.StartsWith("/api/auth", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/api/system/auth", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/api/system/SystemConfigs/system", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/api/identity/connect", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("调试：MultiTenant 白名单放行 {Method} {Path}", method, path);
                await _next(context);
                return;
            }

            // 解析门店ID（仅 store 子系统 API 需要，从 X-Store-Id 请求头获取）
            // 注意：必须在 super_admin 跳过分支之前执行，否则超级管理员用户无法获取 StoreId，
            // 导致 store 子系统的 API（如 DashboardAppService）返回"请选择门店"
            if (path.StartsWith("/api/store", StringComparison.OrdinalIgnoreCase))
            {
                var storeIdHeader = context.Request.Headers["X-Store-Id"].FirstOrDefault();
                if (long.TryParse(storeIdHeader, out var storeId))
                {
                    context.Items["StoreId"] = storeId;
                    context.Response.Headers["X-Store-Id"] = storeId.ToString();
                }
            }

            // 超级管理员跳过租户校验（基于角色判断，不依赖用户名硬编码）
            // 与 CurrentUser.IsSuperAdmin 实现保持一致：查找 ClaimTypes.Role 中是否包含 "super_admin"
            var isSuperAdmin = context.User.FindAll(ClaimTypes.Role)
                .Select(c => c.Value)
                .Contains("super_admin");
            if (isSuperAdmin)
            {
                await _next(context);
                return;
            }

            var tenant = await tenantProvider.GetCurrentTenantAsync(context.RequestAborted);

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

            if (tenant.ExpireTime.HasValue && tenant.ExpireTime.Value.Date < DateTime.Now.Date)
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
            timestamp = DateTime.Now
        };
        await JsonSerializer.SerializeAsync(context.Response.Body, response);
    }
}
