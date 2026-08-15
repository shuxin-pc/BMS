using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Bms.Store.Api.Middleware;

/// <summary>
/// 用户状态校验中间件（L2 修复：Store 服务侧）
/// 在认证之后检查用户是否被禁用，弥补 CurrentUser 完全依赖 Claims 导致权限变更不立即生效的缺陷
/// 使用 IMemoryCache 短时缓存（30 秒）避免每次请求查 DB
/// </summary>
/// <remarks>
/// 实现方式与 System.Api 的 UserStatusCheckMiddleware 对等，但通过 Npgsql 直接查询 bms_system."Users" 表
/// （复用 SystemTenantStore 已建立的 SystemDb 连接，不引入 SystemDbContext）
/// 跳过场景：
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
    private readonly string _connectionString;

    private const string UserStatusCacheKeyPrefix = "user_status_";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(30);

    public UserStatusCheckMiddleware(
        RequestDelegate next,
        ILogger<UserStatusCheckMiddleware> logger,
        IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        _connectionString = configuration.GetConnectionString("SystemDb")
            ?? throw new InvalidOperationException("SystemDb 连接字符串未配置");
    }

    public async Task InvokeAsync(HttpContext context, IMemoryCache cache)
    {
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
            // 缓存未命中，查询 bms_system."Users" 表
            isActive = await CheckUserActiveAsync(userId);

            // 缓存 30 秒
            cache.Set(cacheKey, isActive, CacheTtl);
        }

        if (!isActive)
        {
            _logger.LogWarning("用户已被禁用，拒绝访问，UserId={UserId}，Path={Path}",
                userId, context.Request.Path.Value);
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

    /// <summary>
    /// 直接查询 bms_system."Users" 表检查用户状态
    /// 只读取 Status 字段，不涉及敏感数据
    /// </summary>
    private async Task<bool> CheckUserActiveAsync(long userId)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            const string sql = @"
                SELECT ""Status"" FROM bms_system.""Users""
                WHERE ""Id"" = @id AND ""IsDeleted"" = false";

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("id", userId);

            var statusObj = await command.ExecuteScalarAsync();
            // Status: 0=禁用, 1=正常（UserStatus.Normal = 1）
            return statusObj is int status && status == 1;
        }
        catch (Exception ex)
        {
            // 查询失败时 fail-open（放行），避免数据库异常导致所有用户被锁死
            // 用户状态检查是增强校验，不应阻断正常业务
            _logger.LogError(ex, "查询用户状态失败，UserId={UserId}，临时放行", userId);
            return true;
        }
    }
}
