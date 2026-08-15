using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Bms.System.Api.Middleware;

/// <summary>
/// 内部服务调用认证中间件
/// 在 UseAuthentication 之后、UseAuthorization 之前执行
/// 检测 X-Internal-Service 和 X-Internal-Service-Key headers，
/// 匹配则设置超级管理员身份，使后续的授权和多租户中间件能正常工作
/// </summary>
/// <remarks>
/// 安全策略（S1/M6 修复）：
/// - 对 /api/internal/* 路径强制校验内部服务 headers，缺失或不匹配返回 401
/// - 密钥比较使用 CryptographicOperations.FixedTimeEquals 恒定时间比较，防止时序攻击
/// - 未配置 ServiceKey 时 fail-secure，拒绝所有 internal 请求
/// - 非 internal 路径保持原有"可选触发"行为，避免影响其他业务路径
/// </remarks>
public class InternalServiceAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<InternalServiceAuthMiddleware> _logger;

    private const string InternalServiceHeader = "X-Internal-Service";
    private const string InternalServiceKeyHeader = "X-Internal-Service-Key";
    private const string InternalPathPrefix = "/api/internal/";

    public InternalServiceAuthMiddleware(RequestDelegate next, ILogger<InternalServiceAuthMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IConfiguration configuration)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // 内部接口路径强制校验，非内部接口保持原有可选触发行为
        if (path.StartsWith(InternalPathPrefix, StringComparison.OrdinalIgnoreCase))
        {
            if (!await TryAuthorizeInternalService(context, configuration))
            {
                return;
            }
        }
        else
        {
            TryApplyInternalServiceIdentity(context, configuration);
        }

        await _next(context);
    }

    /// <summary>
    /// 强制校验内部服务调用凭据，校验通过则设置超级管理员身份
    /// </summary>
    /// <returns>true 表示校验通过；false 表示已写入 401 响应，应短路返回</returns>
    private async Task<bool> TryAuthorizeInternalService(HttpContext context, IConfiguration configuration)
    {
        var serviceName = context.Request.Headers[InternalServiceHeader].FirstOrDefault();
        var serviceKey = context.Request.Headers[InternalServiceKeyHeader].FirstOrDefault();
        var expectedKey = configuration["InternalServices:ServiceKey"];

        // 未配置内部服务密钥时 fail-secure，拒绝所有 internal 请求
        if (string.IsNullOrEmpty(expectedKey))
        {
            _logger.LogError("内部服务密钥未配置（InternalServices:ServiceKey），拒绝访问内部接口: {Path}", context.Request.Path);
            await WriteUnauthorizedResponse(context, "内部服务认证未配置");
            return false;
        }

        // 缺失服务名或密钥
        if (string.IsNullOrEmpty(serviceName) || string.IsNullOrEmpty(serviceKey))
        {
            _logger.LogWarning("内部接口缺少认证头: {Path}, ServiceName={ServiceName}", context.Request.Path, serviceName ?? "null");
            await WriteUnauthorizedResponse(context, "缺少内部服务认证信息");
            return false;
        }

        // 恒定时间比较密钥，防止时序攻击（M6）
        if (!FixedTimeEqualsKey(serviceKey, expectedKey))
        {
            _logger.LogWarning("内部服务调用认证失败（密钥不匹配）: {Path}, ServiceName={ServiceName}", context.Request.Path, serviceName);
            await WriteUnauthorizedResponse(context, "内部服务认证失败");
            return false;
        }

        SetInternalServiceIdentity(context, serviceName);
        _logger.LogDebug("内部服务调用认证通过: {ServiceName}", serviceName);
        return true;
    }

    /// <summary>
    /// 非 internal 路径的可选触发逻辑：仅在 headers 存在时校验，保持向后兼容
    /// </summary>
    private void TryApplyInternalServiceIdentity(HttpContext context, IConfiguration configuration)
    {
        var serviceName = context.Request.Headers[InternalServiceHeader].FirstOrDefault();
        if (string.IsNullOrEmpty(serviceName))
        {
            return;
        }

        var serviceKey = context.Request.Headers[InternalServiceKeyHeader].FirstOrDefault();
        var expectedKey = configuration["InternalServices:ServiceKey"];

        if (!string.IsNullOrEmpty(expectedKey) && FixedTimeEqualsKey(serviceKey ?? string.Empty, expectedKey))
        {
            SetInternalServiceIdentity(context, serviceName);
            _logger.LogDebug("内部服务调用认证通过: {ServiceName}", serviceName);
        }
        else
        {
            _logger.LogWarning("内部服务调用认证失败: {ServiceName}", serviceName);
        }
    }

    /// <summary>
    /// 设置伪装的超级管理员身份，用于内部服务调用
    /// </summary>
    private static void SetInternalServiceIdentity(HttpContext context, string serviceName)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Role, "super_admin"),
            new Claim("tenant_id", "1"),
            new Claim("internal_service", serviceName)
        };
        var identity = new ClaimsIdentity(claims, "InternalService");
        context.User = new ClaimsPrincipal(identity);
    }

    /// <summary>
    /// 恒定时间比较密钥，防止时序攻击
    /// </summary>
    private static bool FixedTimeEqualsKey(string actual, string expected)
    {
        var actualBytes = Encoding.UTF8.GetBytes(actual);
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        return CryptographicOperations.FixedTimeEquals(actualBytes, expectedBytes);
    }

    /// <summary>
    /// 写入 401 未授权响应
    /// </summary>
    private static async Task WriteUnauthorizedResponse(HttpContext context, string message)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/json";
        var response = new
        {
            code = 401,
            message,
            requestId = context.TraceIdentifier
        };
        await global::System.Text.Json.JsonSerializer.SerializeAsync(context.Response.Body, response);
    }
}
