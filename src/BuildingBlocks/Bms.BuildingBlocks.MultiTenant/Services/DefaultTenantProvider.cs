using Bms.BuildingBlocks.MultiTenant.Abstractions;
using Bms.BuildingBlocks.MultiTenant.Models;
using Microsoft.Extensions.Logging;

namespace Bms.BuildingBlocks.MultiTenant.Services;

/// <summary>
/// 默认租户提供者
/// </summary>
public class DefaultTenantProvider : ITenantProvider
{
    private readonly IEnumerable<ITenantResolutionStrategy> _resolutionStrategies;
    private readonly ITenantStore _tenantStore;
    private readonly ILogger<DefaultTenantProvider> _logger;
    private TenantInfo? _currentTenant;

    public DefaultTenantProvider(
        IEnumerable<ITenantResolutionStrategy> resolutionStrategies,
        ITenantStore tenantStore,
        ILogger<DefaultTenantProvider> logger)
    {
        _resolutionStrategies = resolutionStrategies;
        _tenantStore = tenantStore;
        _logger = logger;
    }

    public async Task<TenantInfo?> GetCurrentTenantAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTenant != null)
        {
            _logger.LogDebug("Using cached tenant: {TenantId}", _currentTenant.Id);
            return _currentTenant;
        }

        // 按优先级尝试所有解析策略
        foreach (var strategy in _resolutionStrategies)
        {
            var identifier = await strategy.ResolveTenantIdentifierAsync(cancellationToken);
            _logger.LogDebug("Strategy {StrategyType} resolved identifier: {Identifier}",
                strategy.GetType().Name, identifier ?? "null");

            if (string.IsNullOrEmpty(identifier))
                continue;

            _currentTenant = await ResolveTenantAsync(identifier, cancellationToken);
            _logger.LogDebug("Resolved tenant for identifier {Identifier}: {TenantId} - {TenantName}",
                identifier, _currentTenant?.Id, _currentTenant?.Name);

            if (_currentTenant != null)
                break;
        }

        if (_currentTenant == null)
        {
            _logger.LogWarning("Could not resolve tenant from any strategy");
        }

        return _currentTenant;
    }

    private async Task<TenantInfo?> ResolveTenantAsync(string identifier, CancellationToken cancellationToken)
    {
        // 先尝试解析为租户ID（long类型）
        if (long.TryParse(identifier, out var tenantId))
        {
            var tenant = await _tenantStore.GetTenantByIdAsync(tenantId, cancellationToken);
            if (tenant != null)
                return tenant;
        }

        // 再尝试解析为租户编码
        return await _tenantStore.GetTenantByCodeAsync(identifier, cancellationToken);
    }
}
