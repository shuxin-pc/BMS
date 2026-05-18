using System.Security.Claims;
using Bms.BuildingBlocks.MultiTenant.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Bms.BuildingBlocks.MultiTenant.Resolvers;

/// <summary>
/// Claims解析策略
/// 从用户Claims中获取租户ID
/// </summary>
public class ClaimTenantResolver : ITenantResolutionStrategy
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<ClaimTenantResolver> _logger;
    private const string TenantClaimType = "tenant_id";

    public ClaimTenantResolver(IHttpContextAccessor httpContextAccessor, ILogger<ClaimTenantResolver> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public Task<string?> ResolveTenantIdentifierAsync(CancellationToken cancellationToken = default)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        _logger.LogInformation("=== ClaimTenantResolver: User.Identity?.IsAuthenticated = {IsAuthenticated}",
            user?.Identity?.IsAuthenticated);

        if (user != null && user.Identity != null && user.Identity.IsAuthenticated)
        {
            _logger.LogInformation("=== ClaimTenantResolver: Available Claims ===");
            foreach (var claim in user.Claims)
            {
                _logger.LogInformation("Claim: {Type} = {Value}", claim.Type, claim.Value);
            }
        }

        var tenantId = _httpContextAccessor.HttpContext?.User.FindFirst(TenantClaimType)?.Value;
        _logger.LogInformation("=== ClaimTenantResolver: Resolved tenant_id = {TenantId}", tenantId ?? "null");

        return Task.FromResult(tenantId);
    }
}
