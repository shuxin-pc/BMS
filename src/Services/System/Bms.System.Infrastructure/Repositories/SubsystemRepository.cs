using Microsoft.EntityFrameworkCore;
using Bms.System.Domain.Entities;
using Bms.System.Domain.IRepositories;

namespace Bms.System.Infrastructure.Repositories;

/// <summary>
/// 子系统仓储实现
/// </summary>
public class SubsystemRepository : ISubsystemRepository
{
    private readonly SystemDbContext _context;

    public SubsystemRepository(SystemDbContext context)
    {
        _context = context;
    }

    public async Task<List<Subsystem>> GetListAsync()
    {
        return await _context.Subsystems
            .Where(s => !s.IsDeleted)
            .OrderBy(s => s.Sort)
            .ThenBy(s => s.CreatedTime)
            .ToListAsync();
    }

    public async Task<Subsystem?> GetByIdAsync(long id)
    {
        return await _context.Subsystems
            .Include(s => s.SubsystemMenus)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);
    }

    public async Task<Subsystem?> GetByCodeAsync(string code)
    {
        return await _context.Subsystems
            .FirstOrDefaultAsync(s => s.Code == code && !s.IsDeleted);
    }

    public async Task<bool> ExistsCodeAsync(string code, long? excludeId = null)
    {
        return await _context.Subsystems
            .AnyAsync(s => s.Code == code && !s.IsDeleted && (excludeId == null || s.Id != excludeId));
    }

    public async Task AddAsync(Subsystem subsystem)
    {
        await _context.Subsystems.AddAsync(subsystem);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Subsystem subsystem)
    {
        subsystem.UpdatedTime = DateTime.UtcNow;
        var entry = _context.Subsystems.Attach(subsystem);
        entry.State = EntityState.Modified;
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(long id)
    {
        var subsystem = await _context.Subsystems.FindAsync(id);
        if (subsystem != null)
        {
            subsystem.IsDeleted = true;
            subsystem.UpdatedTime = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<int> GetUsageCountAsync(long id)
    {
        return await _context.TenantSubsystems
            .Where(ts => ts.SubsystemId == id)
            .CountAsync();
    }
}
