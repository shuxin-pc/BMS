using Bms.System.Application.Dtos;
using Bms.System.Application.Dtos.Tenants;
using Bms.System.Domain.Entities;
using Bms.System.Domain.IRepositories;

namespace Bms.System.Application.Services;

/// <summary>
/// 租户子系统应用服务实现
/// </summary>
public class TenantSubsystemAppService : ITenantSubsystemAppService
{
    private readonly ITenantSubsystemRepository _tenantSubsystemRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IRoleMenuAuthRepository _roleMenuAuthRepository;

    public TenantSubsystemAppService(
        ITenantSubsystemRepository tenantSubsystemRepository,
        IRoleRepository roleRepository,
        IRoleMenuAuthRepository roleMenuAuthRepository)
    {
        _tenantSubsystemRepository = tenantSubsystemRepository;
        _roleRepository = roleRepository;
        _roleMenuAuthRepository = roleMenuAuthRepository;
    }

    public async Task<ApiResponseDto<List<long>>> GetByTenantIdAsync(long tenantId)
    {
        var tenantSubsystems = await _tenantSubsystemRepository.GetByTenantIdAsync(tenantId);
        var subsystemIds = tenantSubsystems.Select(x => x.SubsystemId).ToList();
        return ApiResponseDto<List<long>>.Success(subsystemIds);
    }

    public async Task<ApiResponseDto> AssignSubsystemsAsync(long tenantId, TenantSubsystemAssignDto dto)
    {
        var existingSubsystems = await _tenantSubsystemRepository.GetByTenantIdAsync(tenantId);
        var existingIds = existingSubsystems.Select(x => x.SubsystemId).ToList();
        var newIds = dto.SubsystemIds.Distinct().ToList();

        var toRemove = existingIds.Except(newIds).ToList();
        var toAdd = newIds.Except(existingIds).ToList();

        foreach (var subsystemId in toRemove)
        {
            await RemoveSubsystemInternalAsync(tenantId, subsystemId);
        }

        foreach (var subsystemId in toAdd)
        {
            await AddSubsystemInternalAsync(tenantId, subsystemId);
        }

        return ApiResponseDto.Success(null, "分配成功");
    }

    public async Task<ApiResponseDto> AddSubsystemAsync(long tenantId, long subsystemId)
    {
        var existing = await _tenantSubsystemRepository.GetByTenantIdAsync(tenantId);
        if (existing.Any(x => x.SubsystemId == subsystemId))
        {
            throw new InvalidOperationException("该子系统已分配给租户");
        }

        await AddSubsystemInternalAsync(tenantId, subsystemId);

        return ApiResponseDto.Success(null, "分配成功");
    }

    public async Task<ApiResponseDto> RemoveSubsystemAsync(long tenantId, long subsystemId)
    {
        await RemoveSubsystemInternalAsync(tenantId, subsystemId);
        return ApiResponseDto.Success(null, "取消分配成功");
    }

    public async Task<ApiResponseDto> BatchAddSubsystemsAsync(long tenantId, List<long> subsystemIds)
    {
        var existing = await _tenantSubsystemRepository.GetByTenantIdAsync(tenantId);
        var existingIds = existing.Select(x => x.SubsystemId).ToList();
        var toAdd = subsystemIds.Distinct().Except(existingIds).ToList();

        foreach (var subsystemId in toAdd)
        {
            await AddSubsystemInternalAsync(tenantId, subsystemId);
        }

        return ApiResponseDto.Success(null, "批量分配成功");
    }

    public async Task<ApiResponseDto> BatchRemoveSubsystemsAsync(long tenantId, List<long> subsystemIds)
    {
        foreach (var subsystemId in subsystemIds)
        {
            await RemoveSubsystemInternalAsync(tenantId, subsystemId);
        }

        return ApiResponseDto.Success(null, "批量取消分配成功");
    }

    private async Task AddSubsystemInternalAsync(long tenantId, long subsystemId)
    {
        // 检查是否已存在的关联记录
        var exists = await _tenantSubsystemRepository.ExistsAsync(tenantId, subsystemId);
        if (!exists)
        {
            // 不存在则创建新的关联记录
            var tenantSubsystem = new TenantSubsystem
            {
                TenantId = tenantId,
                SubsystemId = subsystemId,
                CreatedTime = DateTime.UtcNow,
                UpdatedTime = DateTime.UtcNow
            };
            await _tenantSubsystemRepository.AddRangeAsync(new[] { tenantSubsystem });
        }
    }

    private async Task RemoveSubsystemInternalAsync(long tenantId, long subsystemId)
    {
        // 删除租户子系统关联记录
        await _tenantSubsystemRepository.DeleteByTenantIdAndSubsystemIdAsync(tenantId, subsystemId);

        // 删除该租户下所有角色与该子系统菜单的权限关联
        var tenantRoles = await _roleRepository.GetByTenantIdAsync(tenantId);
        if (tenantRoles.Any())
        {
            var roleIds = tenantRoles.Select(r => r.Id).ToList();
            var roleMenuAuths = await _roleMenuAuthRepository.GetByRoleIdsAsync(roleIds);
            var toDelete = roleMenuAuths.Where(rma => rma.SubsystemId == subsystemId).ToList();
            if (toDelete.Any())
            {
                foreach (var rma in toDelete)
                {
                    await _roleMenuAuthRepository.DeleteByRoleIdAndMenuIdAsync(rma.RoleId, rma.MenuId);
                }
            }
        }
    }
}
