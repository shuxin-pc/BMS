using Microsoft.EntityFrameworkCore;
using Bms.System.Domain.Entities;
using Bms.System.Domain.IRepositories;

namespace Bms.System.Infrastructure.Repositories;

public class SystemConfigRepository : ISystemConfigRepository
{
    private readonly SystemDbContext _context;

    public SystemConfigRepository(SystemDbContext context)
    {
        _context = context;
    }

    public async Task<SystemConfig?> GetByIdAsync(long id)
    {
        return await _context.SystemConfigs.FindAsync(id);
    }

    public async Task<SystemConfig?> GetByKeyAsync(string configKey)
    {
        return await _context.SystemConfigs
            .FirstOrDefaultAsync(c => c.ConfigKey == configKey);
    }

    public async Task<List<SystemConfig>> GetListAsync()
    {
        return await _context.SystemConfigs
            .OrderBy(c => c.Sort)
            .ToListAsync();
    }

    public async Task<List<SystemConfig>> GetByGroupAsync(string configGroup)
    {
        return await _context.SystemConfigs
            .Where(c => c.ConfigGroup == configGroup)
            .OrderBy(c => c.Sort)
            .ToListAsync();
    }

    public async Task<List<SystemConfig>> GetPublicConfigsAsync()
    {
        return await _context.SystemConfigs
            .Where(c => c.IsPublic)
            .OrderBy(c => c.Sort)
            .ToListAsync();
    }

    public async Task<SystemConfig> AddAsync(SystemConfig config)
    {
        await _context.SystemConfigs.AddAsync(config);
        await _context.SaveChangesAsync();
        return config;
    }

    public async Task UpdateAsync(SystemConfig config)
    {
        config.UpdatedTime = DateTime.UtcNow;
        _context.SystemConfigs.Update(config);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(long id)
    {
        var config = await _context.SystemConfigs.FindAsync(id);
        if (config != null)
        {
            if (!config.IsEditable)
            {
                throw new InvalidOperationException("系统内置配置不可删除");
            }
            _context.SystemConfigs.Remove(config);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsKeyAsync(string configKey, long? excludeId = null)
    {
        return await _context.SystemConfigs
            .AnyAsync(c => c.ConfigKey == configKey && (excludeId == null || c.Id != excludeId));
    }
}