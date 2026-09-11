using System.Text.Json;
using Bms.BuildingBlocks.Core.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Bms.BuildingBlocks.Core.Interceptors;

/// <summary>
/// 审计日志拦截器
/// 自动收集数据变更的审计日志，延迟到请求结束时统一写入
/// 通过 entityTypeFilter 决定收集哪些实体类型（各服务传入自己的实体基类判断），
/// 未配置时收集所有具有单个 Id 主键的变更实体
/// </summary>
public class AuditLogInterceptor : SaveChangesInterceptor
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = false
    };

    private readonly IAuditLogContext _auditLogContext;
    private readonly Func<Type, bool>? _entityTypeFilter;

    public AuditLogInterceptor(IAuditLogContext auditLogContext, Func<Type, bool>? entityTypeFilter = null)
    {
        _auditLogContext = auditLogContext;
        _entityTypeFilter = entityTypeFilter;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        GenerateAuditLogs(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        GenerateAuditLogs(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    private void GenerateAuditLogs(DbContext? context)
    {
        if (context == null)
        {
            return;
        }

        // 检查是否启用审计日志
        if (!_auditLogContext.IsEnabled)
        {
            return;
        }

        var entries = context.ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added ||
                        e.State == EntityState.Modified ||
                        e.State == EntityState.Deleted)
            .Where(e => ShouldAudit(e.Entity.GetType()))
            .ToList();

        if (!entries.Any())
        {
            return;
        }

        foreach (var entry in entries)
        {
            var auditLog = CreateAuditLog(entry);
            if (auditLog != null)
            {
                // 只收集到待写入列表，不直接写入数据库
                // 响应状态码和耗时由中间件在 OnCompleted 中填充
                _auditLogContext.AddPendingAuditLog(auditLog);
            }
        }
    }

    /// <summary>
    /// 判断实体类型是否需要审计
    /// </summary>
    private bool ShouldAudit(Type entityType)
    {
        if (_entityTypeFilter != null)
        {
            return _entityTypeFilter(entityType);
        }

        // 未配置过滤器时，仅收集具有单个 Id 主键的业务实体（排除联合主键关联表）
        var primaryKey = entityType.GetProperty("Id");
        return primaryKey != null && primaryKey.PropertyType == typeof(long);
    }

    private AuditLogEntry CreateAuditLog(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
    {
        var entityType = entry.Entity.GetType().Name;
        var entityId = GetEntityId(entry);

        string operationType;
        string? entityChanges = null;

        switch (entry.State)
        {
            case EntityState.Added:
                operationType = "Create";
                // 新增时记录表列值。不能直接序列化实体对象：导航属性存在循环引用（如 User -> UserRoles -> User），会抛 JsonException 导致 SaveChanges 整体失败
                entityChanges = JsonSerializer.Serialize(entry.CurrentValues.ToObject(), _jsonOptions);
                break;

            case EntityState.Modified:
                operationType = "Update";
                // 更新时只记录变更的字段，包含字段名、变更前的值、变更后的值
                var modifiedFields = new Dictionary<string, object>();
                foreach (var property in entry.Properties)
                {
                    if (property.Metadata.Name != "Id" && property.IsModified)
                    {
                        modifiedFields[property.Metadata.Name] = new
                        {
                            OldValue = property.OriginalValue?.ToString(),
                            NewValue = property.CurrentValue?.ToString()
                        };
                    }
                }
                entityChanges = JsonSerializer.Serialize(modifiedFields, _jsonOptions);
                break;

            case EntityState.Deleted:
                operationType = "Delete";
                // 删除时记录删除前的完整数据
                entityChanges = JsonSerializer.Serialize(entry.OriginalValues.ToObject(), _jsonOptions);
                break;

            default:
                operationType = "Unknown";
                break;
        }

        return new AuditLogEntry
        {
            // 用户信息 - 从审计日志上下文获取
            TenantId = _auditLogContext.TenantId,
            StoreId = _auditLogContext.StoreId,
            StoreName = _auditLogContext.StoreName,
            UserId = _auditLogContext.UserId,
            UserName = _auditLogContext.UserName,
            RealName = _auditLogContext.RealName,

            // 操作信息（操作内容为"动作+实体中文名"，对象ID单独落库，仅在详情中展示）
            OperationType = operationType,
            EntityId = entityId as long?,
            OperationContent = $"{GetOperationTypeLabel(operationType)}{AuditLogEntityNames.GetDisplayName(entityType)}",

            // 请求信息
            RequestPath = _auditLogContext.RequestPath,
            RequestMethod = _auditLogContext.RequestMethod,
            RequestIp = _auditLogContext.RequestIp,
            UserAgent = _auditLogContext.UserAgent,

            // 响应信息 - 暂时留空，由中间件 OnCompleted 填充
            ResponseStatus = null,
            Duration = null,

            // 实体变更内容
            EntityChanges = entityChanges,

            // 时间戳
            CreatedTime = DateTime.Now
        };
    }

    /// <summary>
    /// 获取实体主键值（兼容不同基类，取名为 Id 的主键属性）
    /// </summary>
    private static object? GetEntityId(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
    {
        var idProperty = entry.Metadata.FindPrimaryKey()?.Properties.FirstOrDefault();
        if (idProperty == null)
        {
            return null;
        }

        return entry.State == EntityState.Deleted
            ? entry.OriginalValues[idProperty]
            : entry.Property(idProperty.Name).CurrentValue;
    }

    /// <summary>
    /// 操作类型的中文标签（未识别的类型回退原值）
    /// </summary>
    private static string GetOperationTypeLabel(string operationType) => operationType switch
    {
        "Create" => "新增",
        "Update" => "修改",
        "Delete" => "删除",
        _ => operationType
    };
}
