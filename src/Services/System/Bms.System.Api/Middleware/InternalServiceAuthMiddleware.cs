using System.Security.Claims;
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
public class InternalServiceAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<InternalServiceAuthMiddleware> _logger;

    private const string InternalServiceHeader = "X-Internal-Service";
    private const string InternalServiceKeyHeader = "X-Internal-Service-Key";

    public InternalServiceAuthMiddleware(RequestDelegate next, ILogger<InternalServiceAuthMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IConfiguration configuration)
    {
        var serviceName = context.Request.Headers[InternalServiceHeader].FirstOrDefault();
        var serviceKey = context.Request.Headers[InternalServiceKeyHeader].FirstOrDefault();

        if (!string.IsNullOrEmpty(serviceName))
        {
            var expectedKey = configuration["InternalServices:ServiceKey"];

            if (!string.IsNullOrEmpty(expectedKey) && serviceKey == expectedKey)
            {
                // 设置伪装的超级管理员身份，用于内部服务调用
                var claims = new[]
                {
                    new Claim(ClaimTypes.Role, "super_admin"),
                    new Claim("tenant_id", "1"),
                    new Claim("internal_service", serviceName)
                };
                var identity = new ClaimsIdentity(claims, "InternalService");
                context.User = new ClaimsPrincipal(identity);

                _logger.LogDebug("内部服务调用认证通过: {ServiceName}", serviceName);
            }
            else
            {
                _logger.LogWarning("内部服务调用认证失败: {ServiceName}", serviceName);
            }
        }

        await _next(context);
    }
}
