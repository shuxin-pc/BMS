using Bms.BuildingBlocks.MultiTenant.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Bms.BuildingBlocks.MultiTenant.Data;

/// <summary>
/// 租户查询过滤器拦截器
/// 为实现ITenant接口的实体自动添加租户ID过滤条件
/// </summary>
public class TenantQueryFilterInterceptor : ISaveChangesInterceptor
{
    private readonly ITenantProvider _tenantProvider;

    public TenantQueryFilterInterceptor(ITenantProvider tenantProvider)
    {
        _tenantProvider = tenantProvider;
    }

    public InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        return result;
    }

    public ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(result);
    }

    public int SavedChanges(
        SaveChangesCompletedEventData eventData,
        int result)
    {
        return result;
    }

    public ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(result);
    }

    public void SaveChangesFailed(DbContextErrorEventData eventData)
    {
    }

    public Task SaveChangesFailedAsync(
        DbContextErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
