using Microsoft.EntityFrameworkCore;
using Bms.System.Domain.Entities;
using Bms.System.Domain.IRepositories;

namespace Bms.System.Infrastructure.Repositories;

public class DataPermissionRepository : IDataPermissionRepository
{
    private readonly SystemDbContext _context;

    public DataPermissionRepository(SystemDbContext context)
    {
        _context = context;
    }

    public async Task<DataPermission?> GetByIdAsync(long id)
    {
        return await _context.DataPermissions
            .Include(d => d.Role)
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<DataPermission?> GetByRoleIdAsync(long roleId)
    {
        return await _context.DataPermissions
            .Include(d => d.Role)
            .FirstOrDefaultAsync(d => d.RoleId == roleId);
    }

    public async Task<List<DataPermission>> GetListAsync()
    {
        return await _context.DataPermissions
            .Include(d => d.Role)
            .ToListAsync();
    }

    public async Task<DataPermission> AddAsync(DataPermission dataPermission)
    {
        await _context.DataPermissions.AddAsync(dataPermission);
        await _context.SaveChangesAsync();
        return dataPermission;
    }

    public async Task UpdateAsync(DataPermission dataPermission)
    {
        dataPermission.UpdatedTime = DateTime.Now;
        _context.DataPermissions.Update(dataPermission);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(long id)
    {
        var dp = await _context.DataPermissions.FindAsync(id);
        if (dp != null)
        {
            _context.DataPermissions.Remove(dp);
            await _context.SaveChangesAsync();
        }
    }

    public async Task DeleteByRoleIdAsync(long roleId)
    {
        var dp = await _context.DataPermissions
            .FirstOrDefaultAsync(d => d.RoleId == roleId);
        if (dp != null)
        {
            _context.DataPermissions.Remove(dp);
            await _context.SaveChangesAsync();
        }
    }
}