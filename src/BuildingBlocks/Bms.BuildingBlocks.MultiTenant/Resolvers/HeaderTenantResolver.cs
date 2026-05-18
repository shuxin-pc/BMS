using Bms.BuildingBlocks.MultiTenant.Abstractions;
using Microsoft.AspNetCore.Http;

namespace Bms.BuildingBlocks.MultiTenant.Resolvers;

/// <summary>
/// 请求头解析策略
/// 从X-Tenant-Code请求头获取租户编码
/// </summary>
public class HeaderTenantResolver : ITenantResolutionStrategy
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private const string TenantHeader = "X-Tenant-Code";

    public HeaderTenantResolver(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Task<string?> ResolveTenantIdentifierAsync(CancellationToken cancellationToken = default)
    {
        var tenantCode = _httpContextAccessor.HttpContext?.Request.Headers[TenantHeader].FirstOrDefault();
        return Task.FromResult(tenantCode);
    }
}
