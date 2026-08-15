using Bms.BuildingBlocks.Core.IdGenerator;
using Bms.Store.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Bms.Store.Infrastructure.Interceptors;

/// <summary>
/// 雪花ID生成拦截器
/// </summary>
public class IdGenerationInterceptor : SaveChangesInterceptor
{
    private readonly IServiceProvider _serviceProvider;

    public IdGenerationInterceptor(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is null) return base.SavingChangesAsync(eventData, result, cancellationToken);

        SetIds(eventData.Context.ChangeTracker.Entries());
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void SetIds(IEnumerable<EntityEntry> entries)
    {
        var idGenerator = _serviceProvider.GetRequiredService<ISnowflakeIdGenerator>();

        foreach (var entry in entries)
        {
            if (entry.State != EntityState.Added) continue;

            // 核心表（继承 StoreBaseEntity，含软删除）
            if (entry.Entity is StoreBaseEntity softDeleteEntity && softDeleteEntity.Id == 0)
            {
                softDeleteEntity.Id = idGenerator.NewId();
            }
            // 非核心表（继承 StoreEntityBase，不含软删除）
            else if (entry.Entity is StoreEntityBase plainEntity && plainEntity.Id == 0)
            {
                plainEntity.Id = idGenerator.NewId();
            }
        }
    }
}

/// <summary>
/// 软删除拦截器
/// </summary>
public class SoftDeleteInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is null) return base.SavingChangesAsync(eventData, result, cancellationToken);

        // 永久删除场景：DbContext 显式声明跳过软删除，直接执行物理 DELETE
        if (eventData.Context is StoreDbContext { SkipSoftDelete: true })
            return base.SavingChangesAsync(eventData, result, cancellationToken);

        SetSoftDeletes(eventData.Context.ChangeTracker.Entries());
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void SetSoftDeletes(IEnumerable<EntityEntry> entries)
    {
        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Deleted && entry.Entity is StoreBaseEntity entity)
            {
                entry.State = EntityState.Modified;
                entity.IsDeleted = true;
                entity.UpdatedTime = DateTime.Now;
            }
        }
    }
}
