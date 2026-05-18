using Bms.System.Domain.Entities;

namespace Bms.System.Domain.IRepositories;

public interface ISystemConfigRepository
{
    Task<SystemConfig?> GetByIdAsync(long id);
    Task<SystemConfig?> GetByKeyAsync(string configKey);
    Task<List<SystemConfig>> GetListAsync();
    Task<List<SystemConfig>> GetByGroupAsync(string configGroup);
    Task<List<SystemConfig>> GetPublicConfigsAsync();
    Task<SystemConfig> AddAsync(SystemConfig config);
    Task UpdateAsync(SystemConfig config);
    Task DeleteAsync(long id);
    Task<bool> ExistsKeyAsync(string configKey, long? excludeId = null);
}