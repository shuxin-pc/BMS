using System.Security.Claims;
using System.Text.Json;
using Bms.Store.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Bms.Store.Api.Middleware;

/// <summary>
/// 门店访问权限校验中间件（M2 修复 + 细粒度授权）
/// 校验 X-Store-Id header 携带的门店 ID 是否在当前用户授权范围内。
/// 授权语义与 <see cref="Bms.Store.Application.Services.StoreAppService.GetAuthorizedStoresAsync"/> 一致：
/// - super_admin：本租户下状态为"营业中"的未删除门店（平台管理员跨租户/门店访问）
/// - 其他用户（含 tenant_admin）：仅在 UserStores 表中被分配且门店营业中的未删除门店
///   tenant_admin 的 UserStore 记录由门店创建时自动维护（AutoAssignTenantAdminUsersAsync）
/// 使用 IMemoryCache 短时缓存（30 秒）避免每次请求查 DB。
/// </summary>
/// <remarks>
/// 跳过场景：
/// - 非 /api/store 路径（X-Store-Id 仅在 store 子系统解析使用）
/// - 未认证请求
/// - super_admin（平台管理员可跨租户/门店访问）
/// - X-Store-Id 缺失或无效（由下游业务逻辑处理"请选择门店"提示）
/// 安全策略：
/// - 校验失败返回 403，防止伪造 X-Store-Id 越权访问其他门店数据
/// - 缓存 30 秒，分配/移除用户后最多 30 秒内生效（与 UserStatusCheckMiddleware 一致）
/// - 查询失败时 fail-open（放行），避免数据库异常导致所有请求被阻断
/// </remarks>
public class StoreAccessMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<StoreAccessMiddleware> _logger;

    private const string StoreAccessCacheKeyPrefix = "store_access_";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(30);

    public StoreAccessMiddleware(
        RequestDelegate next,
        ILogger<StoreAccessMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IMemoryCache cache)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // 仅对 /api/store 路径生效（X-Store-Id 仅在 store 子系统使用）
        if (!path.StartsWith("/api/store", StringComparison.OrdinalIgnoreCase))
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

        var roles = context.User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

        // super_admin 跳过（平台管理员可跨租户/门店访问）
        if (roles.Contains("super_admin"))
        {
            await _next(context);
            return;
        }

        // X-Store-Id 缺失或无效：交给下游处理（业务层会返回"请选择门店"提示）
        var storeIdHeader = context.Request.Headers["X-Store-Id"].FirstOrDefault();
        if (!long.TryParse(storeIdHeader, out var storeId) || storeId <= 0)
        {
            await _next(context);
            return;
        }

        // 获取当前租户 ID（与 MultiTenantMiddleware 中 ClaimTenantResolver 的解析来源保持一致）
        var tenantIdClaim = context.User.FindFirst("tenant_id")?.Value
                            ?? context.User.FindFirst("TenantId")?.Value;
        if (!long.TryParse(tenantIdClaim, out var tenantId) || tenantId <= 0)
        {
            // 无租户信息：交给下游处理
            await _next(context);
            return;
        }

        // 获取当前用户 ID（普通用户走 UserStore 细粒度授权校验）
        var userIdClaim = context.User.FindFirst("user_id")?.Value
                          ?? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? context.User.FindFirst("sub")?.Value;
        if (!long.TryParse(userIdClaim, out var userId) || userId <= 0)
        {
            // 无法识别用户：交给下游处理（避免阻断认证异常的请求）
            await _next(context);
            return;
        }

        // 检查门店访问权限（带缓存，避免每次请求查 DB）
        // 缓存键含 userId：不同用户对同一门店的授权状态独立缓存
        var cacheKey = $"{StoreAccessCacheKeyPrefix}{userId}_{storeId}";
        if (!cache.TryGetValue(cacheKey, out bool isAuthorized))
        {
            using var scope = context.RequestServices.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<StoreDbContext>();
            isAuthorized = await CheckStoreAccessAsync(dbContext, storeId, tenantId, userId);
            cache.Set(cacheKey, isAuthorized, CacheTtl);
        }

        if (!isAuthorized)
        {
            _logger.LogWarning("门店访问被拒绝：UserId={UserId}, StoreId={StoreId}, TenantId={TenantId}, Path={Path}",
                userId, storeId, tenantId, path);
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";
            var response = new
            {
                code = 403,
                message = "无权访问该门店",
                requestId = context.TraceIdentifier
            };
            await JsonSerializer.SerializeAsync(context.Response.Body, response);
            return;
        }

        await _next(context);
    }

    /// <summary>
    /// 查询普通用户对指定门店的访问权限。
    /// 授权条件：门店存在、未删除、属于当前租户、状态为营业中（Status=1），
    /// 且 UserStores 表中存在该用户对该门店的未删除授权记录。
    /// </summary>
    private async Task<bool> CheckStoreAccessAsync(StoreDbContext dbContext, long storeId, long tenantId, long userId)
    {
        try
        {
            return await (from s in dbContext.Stores
                          join us in dbContext.UserStores on s.Id equals us.StoreId
                          where s.Id == storeId
                             && !s.IsDeleted
                             && s.TenantId == tenantId
                             && s.Status == 1
                             && !us.IsDeleted
                             && us.UserId == userId
                          select s).AnyAsync();
        }
        catch (Exception ex)
        {
            // 查询失败时 fail-open（放行），避免数据库异常导致所有请求被阻断
            // 门店访问校验是增强校验，不应阻断正常业务
            _logger.LogError(ex, "查询门店访问权限失败，UserId={UserId}, StoreId={StoreId}, TenantId={TenantId}，临时放行",
                userId, storeId, tenantId);
            return true;
        }
    }
}
