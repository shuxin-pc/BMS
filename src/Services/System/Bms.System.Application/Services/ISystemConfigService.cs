namespace Bms.System.Application.Services;

/// <summary>
/// 系统配置读取服务接口
/// 提供统一的配置读取入口，直接从数据库读取
/// </summary>
public interface ISystemConfigService
{
    /// <summary>
    /// 获取单个配置值
    /// </summary>
    /// <param name="configKey">配置键</param>
    /// <param name="tenantId">租户ID（可选，不传则获取公开配置）</param>
    /// <returns>配置值，不存在返回null</returns>
    Task<string?> GetValueAsync(string configKey, long? tenantId = null);

    /// <summary>
    /// 获取配置值，带默认值
    /// </summary>
    /// <param name="configKey">配置键</param>
    /// <param name="defaultValue">默认值</param>
    /// <param name="tenantId">租户ID（可选）</param>
    /// <returns>配置值或默认值</returns>
    Task<string> GetValueAsync(string configKey, string defaultValue, long? tenantId = null);

    /// <summary>
    /// 获取布尔类型配置
    /// </summary>
    /// <param name="configKey">配置键</param>
    /// <param name="defaultValue">默认值</param>
    /// <param name="tenantId">租户ID（可选）</param>
    Task<bool> GetBoolAsync(string configKey, bool defaultValue = false, long? tenantId = null);

    /// <summary>
    /// 获取整数类型配置
    /// </summary>
    /// <param name="configKey">配置键</param>
    /// <param name="defaultValue">默认值</param>
    /// <param name="tenantId">租户ID（可选）</param>
    Task<int> GetIntAsync(string configKey, int defaultValue = 0, long? tenantId = null);

    /// <summary>
    /// 获取按分组的所有配置
    /// </summary>
    /// <param name="configGroup">配置分组</param>
    /// <param name="tenantId">租户ID（可选）</param>
    /// <returns>配置字典</returns>
    Task<Dictionary<string, string>> GetGroupConfigsAsync(string configGroup, long? tenantId = null);

    /// <summary>
    /// 获取系统配置（System分组）
    /// </summary>
    /// <param name="tenantId">租户ID（可选，用于租户优先级筛选）</param>
    /// <returns>系统配置字典</returns>
    Task<Dictionary<string, string>> GetSystemConfigsAsync(long? tenantId = null);

    /// <summary>
    /// 获取安全策略配置（Security分组）
    /// </summary>
    /// <param name="tenantId">租户ID（可选，用于租户优先级筛选）</param>
    /// <returns>安全策略配置字典</returns>
    Task<Dictionary<string, string>> GetSecurityConfigsAsync(long? tenantId = null);
}
