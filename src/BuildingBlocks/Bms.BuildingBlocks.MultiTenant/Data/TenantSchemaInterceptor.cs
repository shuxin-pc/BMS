using Bms.BuildingBlocks.MultiTenant.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Bms.BuildingBlocks.MultiTenant.Data;

/// <summary>
/// 租户Schema拦截器
/// 用于Schema级隔离，动态切换查询的Schema
/// </summary>
public class TenantSchemaInterceptor : IMaterializationInterceptor, ISaveChangesInterceptor
{
    private readonly ITenantProvider _tenantProvider;

    public TenantSchemaInterceptor(ITenantProvider tenantProvider)
    {
        _tenantProvider = tenantProvider;
    }

    public InterceptionResult<object> CreatingInstance(
        MaterializationInterceptionData materializationData,
        InterceptionResult<object> result)
    {
        // 实体实例化时不需要处理Schema切换，Schema切换在DbContext配置时处理
        return result;
    }

    public InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ApplySchemaMapping(eventData.Context);
        return result;
    }

    public ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ApplySchemaMapping(eventData.Context);
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

    /// <summary>
    /// 应用Schema映射
    /// </summary>
    private void ApplySchemaMapping(DbContext? context)
    {
        if (context == null)
            return;

        var tenant = _tenantProvider.GetCurrentTenantAsync().GetAwaiter().GetResult();
        if (tenant?.IsolationLevel != Models.TenantIsolationLevel.Schema ||
            string.IsNullOrEmpty(tenant.SchemaName))
        {
            return;
        }

        // 动态修改实体的Schema
        foreach (var entityType in context.Model.GetEntityTypes())
        {
            if (typeof(ITenant).IsAssignableFrom(entityType.ClrType) && entityType is IMutableEntityType mutableEntityType)
            {
                mutableEntityType.SetSchema(tenant.SchemaName);
            }
        }
    }
}
