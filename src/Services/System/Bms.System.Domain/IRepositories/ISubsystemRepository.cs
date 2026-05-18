using Bms.System.Domain.Entities;

namespace Bms.System.Domain.IRepositories;

/// <summary>
/// 子系统仓储接口
/// </summary>
public interface ISubsystemRepository
{
    Task<List<Subsystem>> GetListAsync();
    Task<Subsystem?> GetByIdAsync(long id);
    Task<Subsystem?> GetByCodeAsync(string code);
    Task<bool> ExistsCodeAsync(string code, long? excludeId = null);
    Task AddAsync(Subsystem subsystem);
    Task UpdateAsync(Subsystem subsystem);
    Task DeleteAsync(long id);
    Task<int> GetUsageCountAsync(long id);
}
