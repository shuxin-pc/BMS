using System.Security.Claims;
using System.Text.Json;
using Bms.System.Domain.Entities;
using Bms.System.Domain.Enums;
using Bms.System.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Bms.System.Api.Middleware;

/// <summary>
/// 用户状态校验中间件（L2 修复）
/// 在认证之后检查用户是否被禁用，弥补 CurrentUser 完全依赖 Claims 导致权限变更不立即生效的缺陷
/// 使用 IMemoryCache 短时缓存（30 秒）避免每次请求查 DB
/// </summary>
/// <remarks>
/// 跳过场景：
/// - /api/internal/* 内部服务调用（已是 super_admin 身份）
/// - 未认证请求（登录等公开接口）
/// - super_admin（平台管理员状态由超管自己管理）
/// 安全策略：
/// - 用户被禁用后最多 30 秒（缓存 TTL）内生效，返回 401 强制前端重新登录
/// - 配合 H8 刷新令牌机制（刷新时重新加载用户状态），实现权限变更的近实时生效
/// </remarks>
public class UserStatusCheckMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<UserStatusCheckMiddleware> _logger;

    private const string InternalPathPrefix = "/api/internal/";
    private const string UserStatusCacheKeyPrefix = "user_status_";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(30);

    public UserStatusCheckMiddleware(RequestDelegate next, ILogger<UserStatusCheckMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IMemoryCache cache, SystemDbContext dbContext)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // 跳过内部服务调用（InternalServiceAuthMiddleware 已设置 super_admin 身份）
        if (path.StartsWith(InternalPathPrefix, StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        // 跳过未认证请求
        if (!(context.User.Identity?.IsAuthenticated ?? false))
        {
            await _next(context);
            return;
        }

        // super_admin 跳过（平台管理员状态不由此中间件校验）
        var isSuperAdmin = context.User.FindAll(ClaimTypes.Role)
            .Select(c => c.Value)
            .Contains("super_admin");
        if (isSuperAdmin)
        {
            await _next(context);
            return;
        }

        // 获取用户ID
        var userIdClaim = context.User.FindFirst("user_id")?.Value
                          ?? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? context.User.FindFirst("sub")?.Value;
        if (userIdClaim == null || !long.TryParse(userIdClaim, out long userId))
        {
            await _next(context);
            return;
        }

        // 检查用户状态（带缓存，避免每次请求查 DB）
        var cacheKey = $"{UserStatusCacheKeyPrefix}{userId}";
        if (!cache.TryGetValue(cacheKey, out bool isActive))
        {
            // 缓存未命中，查询 DB
            var user = await dbContext.Set<User>().FindAsync(userId);
            isActive = user?.Status == (int)UserStatus.Normal;

            // 缓存 30 秒
            cache.Set(cacheKey, isActive, CacheTtl);
        }

        if (!isActive)
        {
            _logger.LogWarning("用户已被禁用，拒绝访问，UserId={UserId}，Path={Path}", userId, path);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            var response = new
            {
                code = 401,
                message = "用户已被禁用，请重新登录",
                requestId = context.TraceIdentifier
            };
            await JsonSerializer.SerializeAsync(context.Response.Body, response);
            return;
        }

        await _next(context);
    }
}
