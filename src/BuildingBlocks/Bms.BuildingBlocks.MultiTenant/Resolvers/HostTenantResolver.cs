using Bms.BuildingBlocks.MultiTenant.Abstractions;
using Microsoft.AspNetCore.Http;

namespace Bms.BuildingBlocks.MultiTenant.Resolvers;

/// <summary>
/// 域名解析策略
/// 从子域名解析租户编码，如 tenant1.bms.com → tenant1
/// </summary>
public class HostTenantResolver : ITenantResolutionStrategy
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HostTenantResolver(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Task<string?> ResolveTenantIdentifierAsync(CancellationToken cancellationToken = default)
    {
        var host = _httpContextAccessor.HttpContext?.Request.Host.Host;
        if (string.IsNullOrEmpty(host))
            return Task.FromResult<string?>(null);

        // 从子域名解析租户编码
        var parts = host.Split('.');
        if (parts.Length >= 3)
            return Task.FromResult<string?>(parts[0]);

        return Task.FromResult<string?>(null);
    }
}
