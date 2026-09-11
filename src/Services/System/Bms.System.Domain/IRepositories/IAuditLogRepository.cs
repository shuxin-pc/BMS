using Bms.System.Domain.Entities;

namespace Bms.System.Domain.IRepositories;

public interface IAuditLogRepository
{
    Task<AuditLog?> GetByIdAsync(long id);
    Task<List<AuditLog>> GetListAsync();
    Task<List<AuditLog>> GetPagedListAsync(int pageIndex, int pageSize, string? userName = null, string? operationType = null, DateTime? startDate = null, DateTime? endDate = null, string? responseStatus = null, long? tenantId = null);
    Task<int> GetCountAsync(string? userName = null, string? operationType = null, DateTime? startDate = null, DateTime? endDate = null, string? responseStatus = null, long? tenantId = null);
    Task<AuditLog> AddAsync(AuditLog auditLog);
    Task DeleteAsync(long id);
    Task<int> DeleteExpiredAsync(DateTime beforeDate, long? tenantId = null);
}