using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore;
using Bms.Store.Infrastructure;

namespace Bms.Store.Api.Middleware;

/// <summary>
/// 门店上下文解析中间件
/// 在 MultiTenantMiddleware 解析 StoreId 之后执行，查询 Stores 表获取 StoreCode 并写入 HttpContext.Items["StoreCode"]。
/// 供 ICurrentUser.StoreCode 读取，避免业务层每次查库。
/// 使用 IMemoryCache 短时缓存（30 秒）避免每次请求查 DB，与 StoreAccessMiddleware 缓存策略一致。
/// </summary>
/// <remarks>
/// 跳过场景：
/// - 非 /api/store 路径
/// - X-Store-Id 缺失或无效（StoreId 未写入 HttpContext.Items）
/// 查询失败时 fail-open（StoreCode 留空），不阻断请求，与 MultiTenantMiddleware 行为一致。
/// </remarks>
public class StoreContextMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<StoreContextMiddleware> _logger;

    private const string StoreCodeCacheKeyPrefix = "store_code_";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(30);

    public StoreContextMiddleware(RequestDelegate next, ILogger<StoreContextMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IMemoryCache cache)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // 仅对 /api/store 路径生效
        if (!path.StartsWith("/api/store", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        // 读取 MultiTenantMiddleware 写入的 StoreId
        if (context.Items.TryGetValue("StoreId", out var storeIdObj) && storeIdObj is long storeId && storeId > 0)
        {
            var cacheKey = $"{StoreCodeCacheKeyPrefix}{storeId}";
            if (!cache.TryGetValue(cacheKey, out string? storeCode) || storeCode == null)
            {
                try
                {
                    using var scope = context.RequestServices.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<StoreDbContext>();
                    storeCode = await dbContext.Stores
                        .Where(s => s.Id == storeId && !s.IsDeleted)
                        .Select(s => s.Code)
                        .FirstOrDefaultAsync();
                    cache.Set(cacheKey, storeCode ?? string.Empty, CacheTtl);
                }
                catch (Exception ex)
                {
                    // fail-open：查询失败不阻断请求，StoreCode 留空
                    _logger.LogError(ex, "查询门店编码失败，StoreId={StoreId}，临时留空", storeId);
                }
            }

            if (!string.IsNullOrEmpty(storeCode))
            {
                context.Items["StoreCode"] = storeCode;
            }
        }

        await _next(context);
    }
}
