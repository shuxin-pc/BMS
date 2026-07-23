using Bms.BuildingBlocks.MultiTenant.Abstractions;
using Bms.BuildingBlocks.MultiTenant.Models;

namespace Bms.BuildingBlocks.MultiTenant.Stores;

/// <summary>
/// 内存租户存储实现（用于开发测试）
/// </summary>
public class InMemoryTenantStore : ITenantStore
{
    private readonly List<TenantInfo> _tenants = new()
    {
        new TenantInfo
        {
            Id = 1,
            Code = "platform",
            Name = "平台租户",
            IsolationLevel = TenantIsolationLevel.Row,
            IsEnabled = true,
            ExpireTime = DateTime.Now.AddYears(100)
        },
        new TenantInfo
        {
            Id = 2,
            Code = "default",
            Name = "默认租户",
            IsolationLevel = TenantIsolationLevel.Row,
            IsEnabled = true,
            ExpireTime = DateTime.Now.AddYears(10)
        },
        new TenantInfo
        {
            Id = 3,
            Code = "tenant1",
            Name = "测试租户1",
            IsolationLevel = TenantIsolationLevel.Row,
            IsEnabled = true,
            ExpireTime = DateTime.Now.AddYears(1)
        }
    };

    /// <summary>
    /// 根据租户ID获取租户信息
    /// </summary>
    public Task<TenantInfo?> GetTenantByIdAsync(long tenantId, CancellationToken cancellationToken = default)
    {
        var tenant = _tenants.FirstOrDefault(t => t.Id == tenantId);
        return Task.FromResult(tenant);
    }

    /// <summary>
    /// 根据租户编码获取租户信息
    /// </summary>
    public Task<TenantInfo?> GetTenantByCodeAsync(string tenantCode, CancellationToken cancellationToken = default)
    {
        var tenant = _tenants.FirstOrDefault(t => t.Code.Equals(tenantCode, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(tenant);
    }

    /// <summary>
    /// 获取所有租户信息
    /// </summary>
    public Task<IEnumerable<TenantInfo>> GetAllTenantsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<TenantInfo>>(_tenants);
    }

    /// <summary>
    /// 更新租户信息
    /// </summary>
    public Task<bool> UpdateTenantAsync(TenantInfo tenantInfo, CancellationToken cancellationToken = default)
    {
        var index = _tenants.FindIndex(t => t.Id == tenantInfo.Id);
        if (index < 0)
        {
            return Task.FromResult(false);
        }
        _tenants[index] = tenantInfo;
        return Task.FromResult(true);
    }

    /// <summary>
    /// 添加租户
    /// </summary>
    public Task<long> AddTenantAsync(TenantInfo tenantInfo, CancellationToken cancellationToken = default)
    {
        var newId = _tenants.Any() ? _tenants.Max(t => t.Id) + 1 : 1;
        tenantInfo.Id = newId;
        _tenants.Add(tenantInfo);
        return Task.FromResult(newId);
    }

    /// <summary>
    /// 删除租户
    /// </summary>
    public Task<bool> DeleteTenantAsync(long tenantId, CancellationToken cancellationToken = default)
    {
        var tenant = _tenants.FirstOrDefault(t => t.Id == tenantId);
        if (tenant == null)
        {
            return Task.FromResult(false);
        }
        _tenants.Remove(tenant);
        return Task.FromResult(true);
    }

    /// <summary>
    /// 批量删除租户
    /// </summary>
    public Task<int> BatchDeleteAsync(IEnumerable<long> tenantIds, CancellationToken cancellationToken = default)
    {
        var ids = tenantIds.ToList();
        var toRemove = _tenants.Where(t => ids.Contains(t.Id)).ToList();
        foreach (var tenant in toRemove)
        {
            _tenants.Remove(tenant);
        }
        return Task.FromResult(toRemove.Count);
    }
}
