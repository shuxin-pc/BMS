using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using System.Security.Claims;
using Bms.Identity.Api.Models;
using Bms.Identity.Api.Services;

namespace Bms.Identity.Api.Controllers;

/// <summary>
/// OpenIddict 授权控制器
/// </summary>
[ApiController]
public class AuthorizationController : ControllerBase
{
    private readonly ISystemApiClient _systemApiClient;
    private readonly ILogger<AuthorizationController> _logger;

    public AuthorizationController(
        ISystemApiClient systemApiClient,
        ILogger<AuthorizationController> logger)
    {
        _systemApiClient = systemApiClient;
        _logger = logger;
    }

    /// <summary>
    /// 令牌端点（/connect/token）
    /// </summary>
    [HttpPost("~/connect/token")]
    [Produces("application/json")]
    public async Task<IActionResult> Exchange()
    {
        var request = HttpContext.GetOpenIddictServerRequest();

        _logger.LogInformation("令牌端点被调用，授权类型：{GrantType}，用户名：{Username}",
            request?.GrantType, request?.Username);

        if (request == null)
        {
            _logger.LogWarning("出现错误：令牌请求无效，请求为空");
            return BadRequest(new OpenIddictResponse
            {
                Error = OpenIddictConstants.Errors.InvalidRequest,
                ErrorDescription = "无效的请求"
            });
        }

        // 处理密码模式
        if (request.GrantType == OpenIddictConstants.GrantTypes.Password)
        {
            return await HandlePasswordFlowAsync(request);
        }

        // 处理刷新令牌模式
        if (request.GrantType == OpenIddictConstants.GrantTypes.RefreshToken)
        {
            return await HandleRefreshTokenFlowAsync();
        }

        _logger.LogWarning("出现错误：不支持的授权类型：{GrantType}", request.GrantType);
        return BadRequest(new OpenIddictResponse
        {
            Error = OpenIddictConstants.Errors.UnsupportedGrantType,
            ErrorDescription = "不支持的授权类型"
        });
    }

    private async Task<IActionResult> HandlePasswordFlowAsync(OpenIddictRequest request)
    {
        _logger.LogInformation("处理密码模式授权，用户：{Username}", request.Username);

        if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
        {
            _logger.LogWarning("出现错误：用户名或密码为空");
            return BadRequest(new OpenIddictResponse
            {
                Error = OpenIddictConstants.Errors.InvalidRequest,
                ErrorDescription = "用户名和密码不能为空"
            });
        }

        try
        {
            // 调用 System.Api 验证用户
            _logger.LogInformation("正在调用 System.Api 验证用户...");
            var validationResult = await _systemApiClient.ValidateUserAsync(request.Username, request.Password);


            if (!validationResult.IsValid)
            {
                _logger.LogWarning("出现错误：用户验证失败，用户名：{Username}，错误信息：{Error}", request.Username, validationResult.ErrorMessage);

                // 记录失败的登录审计日志
                await RecordLoginAuditAsync("Login", 0, request.Username, null, 0, GetClientIpAddress(), Request.Headers.UserAgent.ToString(), 401, "/connect/token");


                // 返回 OpenIddict 响应
                return BadRequest(new OpenIddictResponse
                {
                    Error = OpenIddictConstants.Errors.InvalidGrant,
                    ErrorDescription = validationResult.ErrorMessage ?? "用户名或密码错误"
                });
            }

            _logger.LogInformation("用户验证成功：用户名={Username}，用户ID={UserId}，租户ID={TenantId}",
                request.Username, validationResult.UserId, validationResult.TenantId);

            // 记录成功的登录审计日志
            await RecordLoginAuditAsync("Login", validationResult.UserId, validationResult.UserName, validationResult.RealName, validationResult.TenantId, GetClientIpAddress(), Request.Headers.UserAgent.ToString(), 200, "/connect/token");

            // 更新用户最后登录信息
            await UpdateLastLoginAsync(validationResult.UserId, DateTime.Now, GetClientIpAddress());

            // 创建身份
            var identity = new ClaimsIdentity(
                authenticationType: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                nameType: OpenIddictConstants.Claims.Name,
                roleType: OpenIddictConstants.Claims.Role);

            // 添加标准声明
            identity.AddClaim(OpenIddictConstants.Claims.Subject, validationResult.UserId.ToString());
            identity.AddClaim(OpenIddictConstants.Claims.Name, validationResult.UserName);
            identity.AddClaim("tenant_id", validationResult.TenantId.ToString());
            if (!string.IsNullOrEmpty(validationResult.TenantCode))
            {
                identity.AddClaim("tenant_code", validationResult.TenantCode);
            }
            identity.AddClaim("user_id", validationResult.UserId.ToString());

            if (!string.IsNullOrEmpty(validationResult.RealName))
            {
                identity.AddClaim("real_name", validationResult.RealName);
            }

            if (!string.IsNullOrEmpty(validationResult.Email))
            {
                identity.AddClaim(OpenIddictConstants.Claims.Email, validationResult.Email);
            }

            // 添加角色声明
            if (validationResult.Roles != null)
            {
                foreach (var role in validationResult.Roles)
                {
                    identity.AddClaim(OpenIddictConstants.Claims.Role, role);
                }
            }

            // 创建票证
            var principal = new ClaimsPrincipal(identity);
            principal.SetScopes(
                OpenIddictConstants.Scopes.OpenId,
                OpenIddictConstants.Scopes.Profile,
                OpenIddictConstants.Scopes.OfflineAccess);

            principal.SetDestinations(GetDestinations);

            _logger.LogInformation("正在登录用户...");
            return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "出现错误：处理密码模式授权时发生异常，用户：{Username}", request.Username);
            return BadRequest(new OpenIddictResponse
            {
                Error = OpenIddictConstants.Errors.ServerError,
                ErrorDescription = $"服务器内部错误: {ex.Message}"
            });
        }
    }

    private Task<IActionResult> HandleRefreshTokenFlowAsync()
    {
        // OpenIddict 会自动处理刷新令牌逻辑
        // 这里可以添加自定义逻辑，如检查用户是否被禁用等
        return Task.FromResult<IActionResult>(Challenge(
            authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
            properties: new AuthenticationProperties(new Dictionary<string, string?>
            {
                [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.InvalidGrant,
                [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "刷新令牌功能待实现"
            })));
    }

    private static IEnumerable<string> GetDestinations(Claim claim)
    {
        // 根据声明类型确定目标（访问令牌/ID令牌）
        return claim.Type switch
        {
            OpenIddictConstants.Claims.Subject or
            OpenIddictConstants.Claims.Name or
            "tenant_id" or
            "user_id" or
            "real_name" => new[]
            {
                OpenIddictConstants.Destinations.AccessToken,
                OpenIddictConstants.Destinations.IdentityToken
            },
            OpenIddictConstants.Claims.Role => new[]
            {
                OpenIddictConstants.Destinations.AccessToken
            },
            OpenIddictConstants.Claims.Email => new[]
            {
                OpenIddictConstants.Destinations.IdentityToken
            },
            _ => new[] { OpenIddictConstants.Destinations.AccessToken }
        };
    }

    /// <summary>
    /// 记录登录审计日志
    /// </summary>
    private async Task RecordLoginAuditAsync(string operationType, long userId, string? userName, string? realName, long tenantId, string? ip, string? userAgent, int responseStatus, string requestPath)
    {
        try
        {
            var auditRequest = new LoginAuditRequest
            {
                OperationType = operationType,
                UserId = userId,
                UserName = userName,
                RealName = realName,
                TenantId = tenantId > 0 ? tenantId : null,
                RequestIp = ip,
                UserAgent = userAgent,
                ResponseStatus = responseStatus,
                RequestPath = requestPath
            };

            await _systemApiClient.RecordLoginAuditAsync(auditRequest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "记录登录审计日志失败");
        }
    }

    /// <summary>
    /// 更新用户最后登录信息
    /// </summary>
    private async Task UpdateLastLoginAsync(long userId, DateTime lastLoginTime, string? ip)
    {
        try
        {
            await _systemApiClient.UpdateLastLoginAsync(userId, lastLoginTime, ip);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新最后登录信息失败");
        }
    }

    /// <summary>
    /// 获取客户端IP地址
    /// </summary>
    private string? GetClientIpAddress()
    {
        var forwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        var realIp = Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }
}
