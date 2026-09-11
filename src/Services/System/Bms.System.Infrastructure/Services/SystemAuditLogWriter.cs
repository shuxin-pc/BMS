using Bms.BuildingBlocks.Core.Context;
using Bms.System.Domain.Entities;
using Bms.System.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Bms.System.Infrastructure.Services;

/// <summary>
/// System 服务审计日志写入器
/// 将审计日志写入 System 库的 AuditLog 表（全平台审计日志的统一存储）
/// </summary>
public class SystemAuditLogWriter : IAuditLogWriter
{
    private readonly SystemDbContext _dbContext;

    public SystemAuditLogWriter(SystemDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task WriteAsync(IReadOnlyList<AuditLogEntry> entries, int responseStatus, long duration, CancellationToken cancellationToken = default)
    {
        foreach (var entry in entries)
        {
            _dbContext.Set<AuditLog>().Add(new AuditLog
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
