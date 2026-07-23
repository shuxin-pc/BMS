using Microsoft.EntityFrameworkCore;
using Bms.System.Domain.Entities;
using Bms.System.Domain.IRepositories;

namespace Bms.System.Infrastructure.Repositories;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly SystemDbContext _context;

    public AuditLogRepository(SystemDbContext context)
    {
        _context = context;
    }

    public async Task<AuditLog?> GetByIdAsync(long id)
    {
        return await _context.AuditLogs.FindAsync(id);
    }

    public async Task<List<AuditLog>> GetListAsync()
    {
        return await _context.AuditLogs
            .OrderByDescending(a => a.CreatedTime)
            .ToListAsync();
    }

    public async Task<List<AuditLog>> GetPagedListAsync(int pageIndex, int pageSize, string? userName = null, string? operationType = null, DateTime? startDate = null, DateTime? endDate = null, string? responseStatus = null, long? tenantId = null)
    {
        var query = _context.AuditLogs.AsQueryable();

        if (!string.IsNullOrEmpty(userName))
        {
            query = query.Where(a => a.UserName != null && a.UserName.ToLower().Contains(userName.ToLower()));
        }

        if (!string.IsNullOrEmpty(operationType))
        {
            query = query.Where(a => a.OperationType == operationType);
        }

        if (startDate.HasValue)
        {
            query = query.Where(a => a.CreatedTime >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            // 如果结束日期是当天的00:00:00（无时间部分），则设置为当天23:59:59
            var endDateValue = endDate.Value;
            if (endDateValue.Hour == 0 && endDateValue.Minute == 0 && endDateValue.Second == 0)
            {
                endDateValue = endDateValue.AddDays(1).AddTicks(-1);
            }
            query = query.Where(a => a.CreatedTime <= endDateValue);
        }

        if (!string.IsNullOrEmpty(responseStatus))
        {
            if (responseStatus == "success")
            {
                query = query.Where(a => a.ResponseStatus >= 200 && a.ResponseStatus < 300);
            }
            else if (responseStatus == "error")
            {
                query = query.Where(a => a.ResponseStatus >= 400);
            }
        }

        if (tenantId.HasValue)
        {
            query = query.Where(a => a.TenantId == tenantId.Value);
        }

        return await query
            .OrderByDescending(a => a.CreatedTime)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<AuditLog> AddAsync(AuditLog auditLog)
    {
        await _context.AuditLogs.AddAsync(auditLog);
        await _context.SaveChangesAsync();
        return auditLog;
    }

    public async Task DeleteAsync(long id)
    {
        var log = await _context.AuditLogs.FindAsync(id);
        if (log != null)
        {
            _context.AuditLogs.Remove(log);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<int> DeleteExpiredAsync(DateTime beforeDate)
    {
        var expiredLogs = await _context.AuditLogs
            .Where(a => a.CreatedTime < beforeDate)
            .ToListAsync();

        _context.AuditLogs.RemoveRange(expiredLogs);
        await _context.SaveChangesAsync();
        return expiredLogs.Count;
    }

    public async Task<int> GetCountAsync(string? userName = null, string? operationType = null, DateTime? startDate = null, DateTime? endDate = null, string? responseStatus = null, long? tenantId = null)
    {
        var query = _context.AuditLogs.AsQueryable();

        if (!string.IsNullOrEmpty(userName))
        {
            query = query.Where(a => a.UserName != null && a.UserName.ToLower().Contains(userName.ToLower()));
        }

        if (!string.IsNullOrEmpty(operationType))
        {
            query = query.Where(a => a.OperationType == operationType);
        }

        if (startDate.HasValue)
        {
            query = query.Where(a => a.CreatedTime >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            // 如果结束日期是当天的00:00:00（无时间部分），则设置为当天23:59:59
            var endDateValue = endDate.Value;
            if (endDateValue.Hour == 0 && endDateValue.Minute == 0 && endDateValue.Second == 0)
            {
                endDateValue = endDateValue.AddDays(1).AddTicks(-1);
            }
            query = query.Where(a => a.CreatedTime <= endDateValue);
        }

        if (!string.IsNullOrEmpty(responseStatus))
        {
            if (responseStatus == "success")
            {
                query = query.Where(a => a.ResponseStatus >= 200 && a.ResponseStatus < 300);
            }
            else if (responseStatus == "error")
            {
                query = query.Where(a => a.ResponseStatus >= 400);
            }
        }

        if (tenantId.HasValue)
        {
            query = query.Where(a => a.TenantId == tenantId.Value);
        }

        return await query.CountAsync();
    }
}