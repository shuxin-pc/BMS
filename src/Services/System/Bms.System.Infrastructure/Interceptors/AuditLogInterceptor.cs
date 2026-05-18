using System.Text.Json;
using Bms.BuildingBlocks.Core.Context;
using Bms.System.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Bms.System.Infrastructure.Interceptors;

/// <summary>
/// 审计日志拦截器
/// 自动收集数据变更的审计日志，延迟到请求结束时统一写入
/// </summary>
public class AuditLogInterceptor : SaveChangesInterceptor
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = false
    };

    private readonly IAuditLogContext _auditLogContext;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditLogInterceptor(IAuditLogContext auditLogContext, IHttpContextAccessor httpContextAccessor)
    {
        _auditLogContext = auditLogContext;
        _httpContextAccessor = httpContextAccessor;
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

        var entries = context.ChangeTracker.Entries<BaseEntity>()
            .Where(e => e.State == EntityState.Added ||
                        e.State == EntityState.Modified ||
                        e.State == EntityState.Deleted)
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

    private AuditLogEntry CreateAuditLog(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<BaseEntity> entry)
    {
        var entityType = entry.Entity.GetType().Name;
        var entityId = entry.Entity.Id.ToString();

        string operationType;
        string? entityChanges = null;

        switch (entry.State)
        {
            case EntityState.Added:
                operationType = "Create";
                // 新增时记录完整数据
                entityChanges = JsonSerializer.Serialize(entry.Entity, _jsonOptions);
                break;

            case EntityState.Modified:
                operationType = "Update";
                // 更新时只记录变更的字段，包含字段名、变更前的值、变更后的值
                var modifiedFields = new Dictionary<string, object>();
                foreach (var property in entry.Properties)
                {
                    if (property.IsModified)
                    {
                        modifiedFields[property.Metadata.Name] = new
                        {
                            OldValue = entry.OriginalValues[property.Metadata.Name]?.ToString(),
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
            UserId = _auditLogContext.UserId,
            UserName = _auditLogContext.UserName,
            RealName = _auditLogContext.RealName,

            // 操作信息
            OperationType = operationType,
            OperationContent = $"{operationType} {entityType}, Id: {entityId}",

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
            CreatedTime = DateTime.UtcNow
        };
    }
}
