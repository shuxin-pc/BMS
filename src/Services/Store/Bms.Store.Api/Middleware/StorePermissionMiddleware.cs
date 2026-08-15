using Bms.BuildingBlocks.Abstractions.Security;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Bms.Store.Api.Middleware;

/// <summary>
/// Store 服务权限校验中间件
/// 通过 Npgsql 直查 bms_system 表校验权限码（参考 UserQueryService 模式，不引入 SystemDbContext）
/// super_admin 自动放行（从 JWT Claims 读取，平台级管理员拥有所有权限）
/// 其他用户（含 tenant_admin）通过 UserRoles -> RoleMenuAuths -> Menu.PermissionCode 校验
/// tenant_admin 的菜单权限由 super_admin 通过 UI 配置（RoleMenuAuth 表），无种子数据
/// </summary>
public class StorePermissionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<StorePermissionMiddleware> _logger;

    public StorePermissionMiddleware(RequestDelegate next, ILogger<StorePermissionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ICurrentUser currentUser,
        IConfiguration configuration,
        IMemoryCache cache)
    {
        // 未认证请求直接放行（由 [Authorize] 处理）
        var userId = currentUser.UserId;
        if (!userId.HasValue)
        {
            await _next(context);
            return;
        }

        // 获取 Endpoint 的 PermissionAttribute
        var endpoint = context.GetEndpoint();
        if (endpoint == null)
        {
            await _next(context);
            return;
        }

        var permissionAttributes = endpoint.Metadata.GetOrderedMetadata<PermissionAttribute>();
        if (!permissionAttributes.Any())
        {
            // 无权限要求，直接通过
            await _next(context);
            return;
        }

        // super_admin 自动放行（从 JWT Claims 读取，不查数据库）
        // 平台级管理员拥有所有权限，与 System.PermissionMiddleware 行为一致
        if (currentUser.IsSuperAdmin)
        {
            await _next(context);
            return;
        }

        // 其他用户（含 tenant_admin）：查数据库校验权限码
        var requiredCodes = permissionAttributes.Select(a => a.PermissionCode).ToList();
        var cacheKey = $"store_perm_{userId.Value}";
        if (!cache.TryGetValue(cacheKey, out List<string>? userPermissions) || userPermissions == null)
        {
            userPermissions = await LoadUserPermissionsAsync(configuration, userId.Value);
            cache.Set(cacheKey, userPermissions, TimeSpan.FromSeconds(30));
        }

        // 校验权限码（OR 逻辑：满足任一即可）
        var hasPermission = requiredCodes.Any(code =>
            userPermissions.Contains(code, StringComparer.OrdinalIgnoreCase));

        if (!hasPermission)
        {
            _logger.LogWarning(
                "[PermissionDenied] Path={Path}, UserId={UserId}, RequiredPermissions={Required}, UserPermissions={Has}",
                context.Request.Path.Value, userId, string.Join(",", requiredCodes), string.Join(",", userPermissions));
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new
            {
                Code = 403,
                Message = "没有权限访问该资源",
                Path = context.Request.Path.Value,
                RequiredPermissions = requiredCodes
            });
            return;
        }

        await _next(context);
    }

    /// <summary>
    /// 查询用户权限码列表（Npgsql 直查 bms_system 表）
    /// fail-closed：查询失败返回空列表，无权限码即无权限（安全优先）
    /// </summary>
    private async Task<List<string>> LoadUserPermissionsAsync(IConfiguration configuration, long userId)
    {
        var connectionString = configuration.GetConnectionString("SystemDb");
        var permissions = new List<string>();

        try
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();

            const string sql = @"
                SELECT DISTINCT m.""PermissionCode""
                FROM bms_system.""UserRoles"" ur
                JOIN bms_system.""RoleMenuAuths"" rma ON ur.""RoleId"" = rma.""RoleId""
                JOIN bms_system.""Menus"" m ON rma.""MenuId"" = m.""Id""
                WHERE ur.""UserId"" = @userId
                  AND m.""PermissionCode"" IS NOT NULL
                  AND m.""PermissionCode"" <> ''";

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("userId", userId);

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                permissions.Add(reader.GetString(0));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询用户权限码失败，UserId={UserId}（fail-closed：返回空列表）", userId);
        }

        return permissions;
    }
}
