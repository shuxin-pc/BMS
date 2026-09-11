using Bms.BuildingBlocks.Core.Context;
using Bms.System.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bms.Store.Infrastructure.Stores;

/// <summary>
/// Store 服务审计日志写入器
/// 通过 AuditLogDbContext 将审计日志直写 bms_system 库的 AuditLogs 表，
/// 与全平台审计日志统一存储，前端查询页无需跨服务聚合
/// </summary>
public class StoreAuditLogWriter : IAuditLogWriter
{
    private readonly AuditLogDbContext _dbContext;

    public StoreAuditLogWriter(AuditLogDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task WriteAsync(IReadOnlyList<AuditLogEntry> entries, int responseStatus, long duration, CancellationToken cancellationToken = default)
    {
        foreach (var entry in entries)
        {
            _dbContext.AuditLogs.Add(new AuditLog
            {
                TenantId = entry.TenantId,
                StoreId = entry.StoreId,
                StoreName = entry.StoreName,
                UserId = entry.UserId,
                UserName = entry.UserName,
                RealName = entry.RealName,
                OperationType = entry.OperationType,
                OperationContent = entry.OperationContent,
                EntityId = entry.EntityId,
                RequestPath = entry.RequestPath,
                RequestMethod = entry.RequestMethod,
                RequestIp = entry.RequestIp,
                UserAgent = entry.UserAgent,
                ResponseStatus = responseStatus,
                Duration = duration,
                EntityChanges = entry.EntityChanges,
                CreatedTime = entry.CreatedTime ?? DateTime.Now
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
