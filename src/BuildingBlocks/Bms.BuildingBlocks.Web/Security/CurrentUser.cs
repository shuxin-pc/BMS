using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Bms.BuildingBlocks.Abstractions.Security;

namespace Bms.BuildingBlocks.Web.Security;

/// <summary>
/// 当前用户信息实现
/// 从 HttpContext.User Claims 中获取用户信息
/// </summary>
public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public long? UserId
    {
        get
        {
            var claim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? User?.FindFirst("sub")?.Value;
            return long.TryParse(claim, out var id) ? id : null;
        }
    }

    public string? UserName => User?.FindFirst(ClaimTypes.Name)?.Value
                            ?? User?.FindFirst("name")?.Value;

    public string? RealName => User?.FindFirst("real_name")?.Value
                             ?? User?.FindFirst(ClaimTypes.GivenName)?.Value;

    public long? TenantId
    {
        get
        {
            var claim = User?.FindFirst("tenant_id")?.Value;
            return long.TryParse(claim, out var id) ? id : null;
        }
    }

    public string? TenantCode => User?.FindFirst("tenant_code")?.Value;

    /// <summary>
    /// 当前门店ID（从 X-Store-Id 请求头解析，由 MultiTenantMiddleware 写入 HttpContext.Items）
    /// </summary>
    public long? StoreId
    {
        get
        {
            var storeId = _httpContextAccessor.HttpContext?.Items["StoreId"];
            return storeId is long id ? id : null;
        }
    }

    public bool IsSuperAdmin => User?.FindAll(ClaimTypes.Role)
        .Select(c => c.Value)
        .Contains("super_admin") ?? false;

    public bool IsTenantAdmin => User?.FindAll(ClaimTypes.Role)
        .Select(c => c.Value)
        .Contains("tenant_admin") ?? false;

    public IReadOnlyList<string> Roles =>
        User?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList()
        ?? new List<string>();

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;
}
