using Bms.BuildingBlocks.Core.Attributes;
using Bms.BuildingBlocks.Core.IdGenerator;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Bms.System.Infrastructure.Interceptors;

/// <summary>
/// ID生成拦截器
/// 自动为新增实体的Id字段生成雪花ID
/// </summary>
public class IdGenerationInterceptor : SaveChangesInterceptor
{
    private readonly ISnowflakeIdGenerator _idGenerator;

    public IdGenerationInterceptor(ISnowflakeIdGenerator idGenerator)
    {
        _idGenerator = idGenerator;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        GenerateIds(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        GenerateIds(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    private void GenerateIds(DbContext? context)
    {
        if (context == null)
        {
            return;
        }

        var entries = context.ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added);

        foreach (var entry in entries)
        {
            var entityType = entry.Entity.GetType();

            // 检查是否忽略ID生成
            if (entityType.GetCustomAttributes(typeof(IgnoreIdGenerationAttribute), true).Any())
            {
                continue;
            }

            // 获取 Id 属性
            var idProperty = entityType.GetProperty("Id");
            if (idProperty == null || idProperty.PropertyType != typeof(long))
            {
                continue;
            }

            // 只有ID为默认值（0）时才生成
            var idValue = (long?)idProperty.GetValue(entry.Entity);
            if (idValue == 0)
            {
                var newId = _idGenerator.NewId();
                idProperty.SetValue(entry.Entity, newId);
            }
        }
    }
}