using Bms.BuildingBlocks.MultiTenant.Abstractions;

namespace Bms.BuildingBlocks.MultiTenant.Data;

/// <summary>
/// 动态连接字符串解析器
/// </summary>
public class DynamicConnectionStringResolver
{
    private readonly ITenantProvider _tenantProvider;
    private readonly string _baseConnectionString;

    public DynamicConnectionStringResolver(
        ITenantProvider tenantProvider,
        string baseConnectionString)
    {
        _tenantProvider = tenantProvider;
        _baseConnectionString = baseConnectionString;
    }

    /// <summary>
    /// 获取当前租户的连接字符串
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>连接字符串</returns>
    public async Task<string> GetConnectionStringAsync(CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantProvider.GetCurrentTenantAsync(cancellationToken);
        if (tenant == null)
            return _baseConnectionString;

        // 数据库级隔离：使用租户独立连接字符串
        if (tenant.IsolationLevel == Models.TenantIsolationLevel.Database &&
            !string.IsNullOrEmpty(tenant.ConnectionString))
        {
            return tenant.ConnectionString;
        }

        // Schema级和行级隔离：使用基础连接字符串
        return _baseConnectionString;
    }
}
