using Microsoft.Extensions.Logging;
using Bms.System.Domain.IRepositories;

namespace Bms.System.Application.Services;

/// <summary>
/// 系统配置读取服务实现
/// 提供统一的配置读取入口，直接从数据库读取（配置数据量小，无需缓存）
/// </summary>
public class SystemConfigService : ISystemConfigService
{
    private readonly ISystemConfigRepository _systemConfigRepository;
    private readonly ILogger<SystemConfigService> _logger;

    /// <summary>
    /// 平台租户ID（固定值）
    /// </summary>
    private const long PlatformTenantId = 1;

    public SystemConfigService(
        ISystemConfigRepository systemConfigRepository,
        ILogger<SystemConfigService> logger)
    {
        _systemConfigRepository = systemConfigRepository;
        _logger = logger;
    }

    public async Task<string?> GetValueAsync(string configKey, long? tenantId = null)
    {
        var configs = await GetConfigsWithTenantFilterAsync(tenantId);
        return configs.TryGetValue(configKey, out var value) ? value : null;
    }

    public async Task<string> GetValueAsync(string configKey, string defaultValue, long? tenantId = null)
    {
        var value = await GetValueAsync(configKey, tenantId);
        return string.IsNullOrEmpty(value) ? defaultValue : value;
    }

    public async Task<bool> GetBoolAsync(string configKey, bool defaultValue = false, long? tenantId = null)
    {
        var value = await GetValueAsync(configKey, tenantId);
        if (string.IsNullOrEmpty(value))
        {
            return defaultValue;
        }
        return value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("1", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("yes", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<int> GetIntAsync(string configKey, int defaultValue = 0, long? tenantId = null)
    {
        var value = await GetValueAsync(configKey, tenantId);
        if (string.IsNullOrEmpty(value))
        {
            return defaultValue;
        }
        return int.TryParse(value, out var result) ? result : defaultValue;
    }

    public async Task<Dictionary<string, string>> GetGroupConfigsAsync(string configGroup, long? tenantId = null)
    {
        var effectiveTenantId = tenantId ?? PlatformTenantId;
        var allConfigs = await _systemConfigRepository.GetListAsync();

        // 如果指定了分组，先过滤分组
        IEnumerable<Domain.Entities.SystemConfig> filteredByGroup = allConfigs;
        if (!string.IsNullOrEmpty(configGroup))
        {
            filteredByGroup = allConfigs.Where(c => c.ConfigGroup == configGroup);
        }

        // 筛选逻辑：优先返回同租户的私有配置，如果没有则返回公开配置
        return filteredByGroup
            .GroupBy(c => c.ConfigKey.ToLower())
            .Select(g =>
            {
                var tenantPrivateConfig = g.FirstOrDefault(c => c.TenantId == effectiveTenantId && !c.IsPublic);
                if (tenantPrivateConfig != null)
                {
                    return (Key: tenantPrivateConfig.ConfigKey, Value: tenantPrivateConfig.ConfigValue ?? string.Empty);
                }
                var publicConfig = g.FirstOrDefault(c => c.IsPublic);
                return (Key: publicConfig?.ConfigKey ?? g.Key, Value: publicConfig?.ConfigValue ?? string.Empty);
            })
            .Where(x => !string.IsNullOrEmpty(x.Value))
            .ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<Dictionary<string, string>> GetSystemConfigsAsync(long? tenantId = null)
    {
        return await GetGroupConfigsAsync("System", tenantId);
    }

    public async Task<Dictionary<string, string>> GetSecurityConfigsAsync(long? tenantId = null)
    {
        return await GetGroupConfigsAsync("Security", tenantId);
    }

    public void RefreshCache(string? configGroup = null)
    {
        // 无缓存实现，直接记录日志
        _logger.LogDebug("RefreshCache called but caching is disabled");
    }

    public void RegisterChangeCallback(string configKey, Action<string> callback)
    {
        // 无缓存实现，回调暂时保留但不起作用
        _logger.LogDebug("RegisterChangeCallback called but caching is disabled");
    }

    public void NotifyConfigChanged(string configKey, string? newValue)
    {
        // 无缓存实现，直接记录日志
        _logger.LogDebug("NotifyConfigChanged called for {ConfigKey} = {NewValue}, but caching is disabled", configKey, newValue);
    }

    /// <summary>
    /// 获取带租户过滤的配置列表
    /// </summary>
    private async Task<Dictionary<string, string>> GetConfigsWithTenantFilterAsync(long? tenantId)
    {
        var effectiveTenantId = tenantId ?? PlatformTenantId;
        var allConfigs = await _systemConfigRepository.GetListAsync();

        // 筛选逻辑：优先返回同租户的私有配置，如果没有则返回公开配置
        return allConfigs
            .GroupBy(c => c.ConfigKey.ToLower())
            .Select(g =>
            {
                var tenantPrivateConfig = g.FirstOrDefault(c => c.TenantId == effectiveTenantId && !c.IsPublic);
                if (tenantPrivateConfig != null)
                {
                    return (Key: tenantPrivateConfig.ConfigKey, Value: tenantPrivateConfig.ConfigValue ?? string.Empty);
                }
                var publicConfig = g.FirstOrDefault(c => c.IsPublic);
                return (Key: publicConfig?.ConfigKey ?? g.Key, Value: publicConfig?.ConfigValue ?? string.Empty);
            })
            .Where(x => !string.IsNullOrEmpty(x.Value))
            .ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);
    }
}
