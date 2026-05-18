using Microsoft.EntityFrameworkCore;
using Bms.System.Application.Dtos;
using Bms.System.Application.Dtos.Roles;
using Bms.System.Application.Dtos.Menus;
using Bms.System.Domain.Entities;
using Bms.System.Domain.IRepositories;
using Bms.System.Infrastructure;

namespace Bms.System.Application.Services;

public class RoleAppService : IRoleAppService
{
    private readonly IRoleRepository _roleRepository;
    private readonly IMenuRepository _menuRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly IDataPermissionRepository _dataPermissionRepository;
    private readonly SystemDbContext _context;

    public RoleAppService(
        IRoleRepository roleRepository,
        IMenuRepository menuRepository,
        IPermissionRepository permissionRepository,
        IDataPermissionRepository dataPermissionRepository,
        SystemDbContext context)
    {
        _roleRepository = roleRepository;
        _menuRepository = menuRepository;
        _permissionRepository = permissionRepository;
        _dataPermissionRepository = dataPermissionRepository;
        _context = context;
    }

    public async Task<ApiResponseDto<PagedResponseDto<RoleDto>>> GetPagedListAsync(PagedRequestDto request, bool isSuperAdmin = true, long? tenantId = null)
    {
        var allRoles = await _roleRepository.GetListAsync();

        // 租户隔离：非超级管理员只能看到自己租户的角色
        if (!isSuperAdmin && tenantId.HasValue)
        {
            allRoles = allRoles.Where(r => r.TenantId == tenantId.Value).ToList();
        }

        // 超级管理员：支持通过 request.TenantId 筛选指定租户的角色
        if (isSuperAdmin && request.TenantId.HasValue)
        {
            allRoles = allRoles.Where(r => r.TenantId == request.TenantId.Value).ToList();
        }

        // 角色名称筛选（忽略大小写）
        if (!string.IsNullOrEmpty(request.Name))
        {
            allRoles = allRoles.Where(r => r.Name.ToLower().Contains(request.Name.ToLower())).ToList();
        }

        // 角色编码筛选（忽略大小写）
        if (!string.IsNullOrEmpty(request.Code))
        {
            allRoles = allRoles.Where(r => r.Code.ToLower().Contains(request.Code.ToLower())).ToList();
        }

        // 状态筛选
        if (request.Status.HasValue)
        {
            allRoles = allRoles.Where(r => r.Status == request.Status.Value).ToList();
        }

        // 过滤掉系统角色（管理员和普通用户看不到系统角色）
        if (!isSuperAdmin)
        {
            allRoles = allRoles.Where(r => r.Code != "super_admin" && r.Code != "tenant_admin").ToList();
        }

        var totalCount = allRoles.Count;
        var pagedRoles = allRoles.Skip((request.PageIndex - 1) * request.PageSize).Take(request.PageSize).ToList();

        var roleDtos = pagedRoles.Select(r => new RoleDto
        {
            Id = r.Id,
            Name = r.Name,
            Code = r.Code,
            Description = r.Description,
            TenantId = r.TenantId,
            IsSystem = r.IsSystem,
            Status = r.Status,
            Sort = r.Sort,
            DataScopeType = r.DataPermission?.DataScopeType ?? 1,
            CustomOrganizationIds = r.DataPermission?.CustomOrganizationIds,
            CreatedTime = r.CreatedTime,
            UpdatedTime = r.UpdatedTime
        }).ToList();

        var result = new PagedResponseDto<RoleDto>
        {
            List = roleDtos,
            Total = totalCount,
            PageIndex = request.PageIndex,
            PageSize = request.PageSize
        };

        return ApiResponseDto<PagedResponseDto<RoleDto>>.Success(result);
    }

    public async Task<ApiResponseDto<List<RoleDto>>> GetAllListAsync(bool isSuperAdmin = true, long? tenantId = null)
    {
        var roles = await _roleRepository.GetListAsync();

        // 如果明确传了 tenantId，按租户过滤（超级管理员选择租户后加载角色下拉）
        if (tenantId.HasValue)
        {
            roles = roles.Where(r => r.TenantId == tenantId.Value).ToList();
        }
        else
        {
            // 没有明确传租户ID时
            // 非超级管理员：只能看到自己租户的角色，且过滤掉超级管理员角色
            if (!isSuperAdmin)
            {
                roles = roles.Where(r => r.Code != "super_admin").ToList();
            }
            // 超级管理员不传 tenantId 时可以看到所有角色（保留原有行为）
        }

        var roleDtos = roles.Select(r => new RoleDto
        {
            Id = r.Id,
            Name = r.Name,
            Code = r.Code,
            Description = r.Description,
            TenantId = r.TenantId,
            IsSystem = r.IsSystem,
            Status = r.Status,
            Sort = r.Sort,
            DataScopeType = r.DataPermission?.DataScopeType ?? 1,
            CustomOrganizationIds = r.DataPermission?.CustomOrganizationIds,
            CreatedTime = r.CreatedTime,
            UpdatedTime = r.UpdatedTime
        }).ToList();

        return ApiResponseDto<List<RoleDto>>.Success(roleDtos);
    }

    /// <summary>
    /// 获取所有角色列表（不过滤租户，用于跨租户场景）
    /// </summary>
    public async Task<ApiResponseDto<List<RoleDto>>> GetAllListWithoutFilterAsync()
    {
        var roles = await _roleRepository.GetListAsync();

        var roleDtos = roles.Select(r => new RoleDto
        {
            Id = r.Id,
            Name = r.Name,
            Code = r.Code,
            Description = r.Description,
            TenantId = r.TenantId,
            IsSystem = r.IsSystem,
            Status = r.Status,
            Sort = r.Sort,
            DataScopeType = r.DataPermission?.DataScopeType ?? 1,
            CustomOrganizationIds = r.DataPermission?.CustomOrganizationIds,
            CreatedTime = r.CreatedTime,
            UpdatedTime = r.UpdatedTime
        }).ToList();

        return ApiResponseDto<List<RoleDto>>.Success(roleDtos);
    }

    public async Task<ApiResponseDto<RoleDto?>> GetByIdAsync(long id)
    {
        var role = await _roleRepository.GetByIdAsync(id);
        if (role == null)
        {
            return ApiResponseDto<RoleDto?>.Fail("角色不存在", 404);
        }

        var dto = new RoleDto
        {
            Id = role.Id,
            Name = role.Name,
            Code = role.Code,
            Description = role.Description,
            TenantId = role.TenantId,
            IsSystem = role.IsSystem,
            Status = role.Status,
            Sort = role.Sort,
            DataScopeType = role.DataPermission?.DataScopeType ?? 1,
            CustomOrganizationIds = role.DataPermission?.CustomOrganizationIds,
            CreatedTime = role.CreatedTime,
            UpdatedTime = role.UpdatedTime
        };

        return ApiResponseDto<RoleDto?>.Success(dto);
    }

    public async Task<ApiResponseDto<RoleDto>> CreateAsync(RoleCreateDto dto, long currentTenantId, string currentTenantCode)
    {
        if (await _roleRepository.ExistsCodeAsync(dto.Code))
        {
            throw new InvalidOperationException($"角色编码 {dto.Code} 已存在");
        }

        var role = new Role
        {
            Name = dto.Name,
            Code = dto.Code,
            Description = dto.Description,
            Status = dto.Status,
            Sort = dto.Sort,
            // 新增角色时设置当前用户的租户ID
            TenantId = currentTenantId,
            TenantCode = currentTenantCode
        };

        await _roleRepository.AddAsync(role);

        // 添加数据权限
        if (dto.DataScopeType > 1)
        {
            var dataPermission = new DataPermission
            {
                RoleId = role.Id,
                DataScopeType = dto.DataScopeType,
                CustomOrganizationIds = dto.CustomOrganizationIds != null
                    ? string.Join(",", dto.CustomOrganizationIds)
                    : null
            };
            await _dataPermissionRepository.AddAsync(dataPermission);
        }

        // 添加权限
        if (dto.PermissionIds.Any())
        {
            foreach (var permissionId in dto.PermissionIds)
            {
                var rolePermission = new RolePermission
                {
                    RoleId = role.Id,
                    PermissionId = permissionId
                };
                role.RolePermissions.Add(rolePermission);
            }
            await _roleRepository.UpdateAsync(role);
        }

        var result = await GetByIdAsync(role.Id);
        if (result.Data == null)
        {
            throw new InvalidOperationException("创建角色失败");
        }
        return ApiResponseDto<RoleDto>.Success(result.Data, "创建成功");
    }

    public async Task<ApiResponseDto<RoleDto>> UpdateAsync(RoleUpdateDto dto)
    {
        var role = await _roleRepository.GetByIdAsync(dto.Id);
        if (role == null)
        {
            throw new InvalidOperationException("角色不存在");
        }

        if (role.IsSystem)
        {
            throw new InvalidOperationException("系统角色不能修改");
        }

        if (await _roleRepository.ExistsCodeAsync(dto.Code, dto.Id))
        {
            throw new InvalidOperationException($"角色编码 {dto.Code} 已存在");
        }

        // 更新角色基本信息
        role.Name = dto.Name;
        role.Description = dto.Description;
        role.Status = dto.Status;
        role.Sort = dto.Sort;

        // 使用 Attach 更新角色，排除租户ID
        var entry = _context.Roles.Attach(role);
        entry.State = EntityState.Modified;
        entry.Property(e => e.TenantId).IsModified = false;
        entry.Property(e => e.TenantCode).IsModified = false;

        // 更新数据权限（数据范围类型：1-全部数据，2-部门及以下，3-仅本人，4-自定义）
        var existingDataPermission = await _dataPermissionRepository.GetByRoleIdAsync(dto.Id);
        if (existingDataPermission != null)
        {
            existingDataPermission.DataScopeType = dto.DataScopeType;
            // 只有自定义数据范围时才保存组织ID列表
            if (dto.DataScopeType == 4 && dto.CustomOrganizationIds != null && dto.CustomOrganizationIds.Any())
            {
                existingDataPermission.CustomOrganizationIds = string.Join(",", dto.CustomOrganizationIds);
            }
            else
            {
                existingDataPermission.CustomOrganizationIds = null;
            }
            _context.DataPermissions.Update(existingDataPermission);
        }
        else if (dto.DataScopeType > 1)
        {
            var dataPermission = new DataPermission
            {
                RoleId = role.Id,
                DataScopeType = dto.DataScopeType,
                CustomOrganizationIds = (dto.DataScopeType == 4 && dto.CustomOrganizationIds != null && dto.CustomOrganizationIds.Any())
                    ? string.Join(",", dto.CustomOrganizationIds)
                    : null
            };
            await _context.DataPermissions.AddAsync(dataPermission);
        }

        // 一次性保存所有更改
        await _context.SaveChangesAsync();

        var result = await GetByIdAsync(role.Id);
        if (result.Data == null)
        {
            throw new InvalidOperationException("更新角色失败");
        }
        return ApiResponseDto<RoleDto>.Success(result.Data, "更新成功");
    }

    public async Task<ApiResponseDto> DeleteAsync(long id)
    {
        var role = await _roleRepository.GetByIdAsync(id);
        if (role != null && role.IsSystem)
        {
            throw new InvalidOperationException("系统角色不能删除");
        }

        await _roleRepository.DeleteAsync(id);
        return ApiResponseDto.Success(null, "删除成功");
    }

    public async Task<ApiResponseDto> BatchDeleteAsync(List<long> ids)
    {
        if (ids == null || ids.Count == 0)
        {
            return ApiResponseDto.Fail("请选择要删除的角色", 400);
        }

        var deletedCount = 0;
        var systemRoleCount = 0;

        foreach (var id in ids)
        {
            var role = await _roleRepository.GetByIdAsync(id);
            if (role != null && role.IsSystem)
            {
                systemRoleCount++;
                continue;
            }

            await _roleRepository.DeleteAsync(id);
            deletedCount++;
        }

        if (systemRoleCount > 0)
        {
            return ApiResponseDto.Success(null, $"成功删除 {deletedCount} 个角色，{systemRoleCount} 个系统角色被跳过");
        }

        return ApiResponseDto.Success(null, $"成功删除 {deletedCount} 个角色");
    }

    public async Task<ApiResponseDto<List<MenuDto>>> GetRoleMenusAsync(long roleId)
    {
        var menus = await _menuRepository.GetByRoleIdAsync(roleId);
        var result = menus.Select(m => new MenuDto
        {
            Id = m.Id,
            Name = m.Name,
            Code = m.Code,
            Path = m.Path,
            Type = m.Type
        }).ToList();
        return ApiResponseDto<List<MenuDto>>.Success(result);
    }

    public async Task<ApiResponseDto> AssignPermissionsAsync(long roleId, List<long> permissionIds)
    {
        var role = await _roleRepository.GetByIdAsync(roleId);
        if (role == null)
        {
            throw new InvalidOperationException("角色不存在");
        }

        if (role.IsSystem)
        {
            throw new InvalidOperationException("系统角色不能修改权限");
        }

        // 移除现有权限
        role.RolePermissions.Clear();

        // 添加新权限
        foreach (var permissionId in permissionIds)
        {
            role.RolePermissions.Add(new RolePermission
            {
                RoleId = roleId,
                PermissionId = permissionId
            });
        }

        await _roleRepository.UpdateAsync(role);
        return ApiResponseDto.Success(null, "权限分配成功");
    }
}
