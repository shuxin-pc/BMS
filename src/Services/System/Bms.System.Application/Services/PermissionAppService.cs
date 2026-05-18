using Mapster;
using Bms.System.Application.Dtos;
using Bms.System.Application.Dtos.Permissions;
using Bms.System.Domain.Entities;
using Bms.System.Domain.IRepositories;

namespace Bms.System.Application.Services;

public class PermissionAppService : IPermissionAppService
{
    private readonly IPermissionRepository _permissionRepository;
    private readonly IMenuRepository _menuRepository;

    public PermissionAppService(
        IPermissionRepository permissionRepository,
        IMenuRepository menuRepository)
    {
        _permissionRepository = permissionRepository;
        _menuRepository = menuRepository;
    }

    public async Task<PagedResponseDto<PermissionDto>> GetPagedListAsync(PagedRequestDto request)
    {
        var permissions = await _permissionRepository.GetPagedListAsync(request.PageIndex, request.PageSize);
        var allPermissions = await _permissionRepository.GetListAsync();
        var totalCount = allPermissions.Count;

        var permissionDtos = permissions.Select(p => p.Adapt<PermissionDto>()).ToList();

        return new PagedResponseDto<PermissionDto>
        {
            List = permissionDtos,
            Total = totalCount,
            PageIndex = request.PageIndex,
            PageSize = request.PageSize
        };
    }

    public async Task<List<PermissionDto>> GetListByMenuIdAsync(long menuId)
    {
        var permissions = await _permissionRepository.GetByMenuIdAsync(menuId);
        return permissions.Select(p => p.Adapt<PermissionDto>()).ToList();
    }

    public async Task<PermissionDto?> GetByIdAsync(long id)
    {
        var permission = await _permissionRepository.GetByIdAsync(id);
        return permission?.Adapt<PermissionDto>();
    }

    public async Task<PermissionDto> CreateAsync(PermissionCreateDto dto)
    {
        if (await _permissionRepository.ExistsCodeAsync(dto.Code))
        {
            throw new InvalidOperationException($"权限编码 {dto.Code} 已存在");
        }

        var menu = await _menuRepository.GetByIdAsync(dto.MenuId);
        if (menu == null)
        {
            throw new InvalidOperationException($"菜单 {dto.MenuId} 不存在");
        }

        var permission = new Permission
        {
            MenuId = dto.MenuId,
            Name = dto.Name,
            Code = dto.Code,
            Description = dto.Description,
            HttpMethod = dto.HttpMethod,
            ApiPath = dto.ApiPath
        };

        await _permissionRepository.AddAsync(permission);
        return await GetByIdAsync(permission.Id) ?? throw new InvalidOperationException("创建权限失败");
    }

    public async Task<PermissionDto> UpdateAsync(PermissionUpdateDto dto)
    {
        var permission = await _permissionRepository.GetByIdAsync(dto.Id);
        if (permission == null)
        {
            throw new InvalidOperationException("权限不存在");
        }

        if (await _permissionRepository.ExistsCodeAsync(dto.Code, dto.Id))
        {
            throw new InvalidOperationException($"权限编码 {dto.Code} 已存在");
        }

        var menu = await _menuRepository.GetByIdAsync(dto.MenuId);
        if (menu == null)
        {
            throw new InvalidOperationException($"菜单 {dto.MenuId} 不存在");
        }

        permission.MenuId = dto.MenuId;
        permission.Name = dto.Name;
        permission.Code = dto.Code;
        permission.Description = dto.Description;
        permission.HttpMethod = dto.HttpMethod;
        permission.ApiPath = dto.ApiPath;

        await _permissionRepository.UpdateAsync(permission);
        return await GetByIdAsync(permission.Id) ?? throw new InvalidOperationException("更新权限失败");
    }

    public async Task DeleteAsync(long id)
    {
        await _permissionRepository.DeleteAsync(id);
    }
}
