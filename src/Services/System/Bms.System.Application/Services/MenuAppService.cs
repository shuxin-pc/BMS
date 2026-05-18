using Mapster;
using Bms.System.Application.Dtos;
using Bms.System.Application.Dtos.Menus;
using Bms.System.Domain.Entities;
using Bms.System.Domain.IRepositories;

namespace Bms.System.Application.Services;

public class MenuAppService : IMenuAppService
{
    private readonly IMenuRepository _menuRepository;

    public MenuAppService(IMenuRepository menuRepository)
    {
        _menuRepository = menuRepository;
    }

    public async Task<ApiResponseDto<List<MenuDto>>> GetTreeListAsync()
    {
        var menus = await _menuRepository.GetAllMenuTreeAsync();
        var result = MapToTreeDto(menus);
        return ApiResponseDto<List<MenuDto>>.Success(result);
    }

    public async Task<ApiResponseDto<List<MenuDto>>> GetListAsync(MenuQueryDto query)
    {
        var menus = await _menuRepository.GetListAsync();

        // 过滤（忽略大小写）
        if (!string.IsNullOrEmpty(query.Name))
        {
            menus = menus.Where(m => m.Name.ToLower().Contains(query.Name.ToLower())).ToList();
        }

        if (query.Type.HasValue)
        {
            menus = menus.Where(m => m.Type == query.Type.Value).ToList();
        }

        var result = menus.Select(m => m.Adapt<MenuDto>()).ToList();

        return ApiResponseDto<List<MenuDto>>.Success(result);
    }

    public async Task<ApiResponseDto<List<MenuDto>>> GetUserMenusAsync(long userId)
    {
        var menus = await _menuRepository.GetByUserIdAsync(userId);
        var result = MapToTreeDto(menus);
        return ApiResponseDto<List<MenuDto>>.Success(result);
    }

    public async Task<ApiResponseDto<MenuDto?>> GetByIdAsync(long id)
    {
        var menu = await _menuRepository.GetByIdAsync(id);
        if (menu == null)
        {
            return ApiResponseDto<MenuDto?>.Fail("菜单不存在", 404);
        }
        return ApiResponseDto<MenuDto?>.Success(menu.Adapt<MenuDto>());
    }

    public async Task<ApiResponseDto<MenuDto>> CreateAsync(MenuCreateDto dto)
    {
        // 按钮类型：Code 和 PermissionCode 都存权限标识（用户只在权限标识输入框输入）
        string code;
        string permissionCode;
        if (dto.Type == 2)
        {
            // 按钮类型：优先使用 PermissionCode，如果没有则用 Code
            code = !string.IsNullOrEmpty(dto.PermissionCode) ? dto.PermissionCode : dto.Code;
            permissionCode = code;
        }
        else
        {
            code = dto.Code;
            permissionCode = dto.PermissionCode;
        }

        if (await _menuRepository.ExistsCodeAsync(code))
        {
            throw new InvalidOperationException($"菜单编码 {code} 已存在");
        }

        var menu = new Menu
        {
            ParentId = dto.ParentId,
            Name = dto.Name,
            Code = code,
            Path = dto.Path,
            Component = dto.Component,
            Icon = dto.Icon,
            Sort = dto.Sort,
            Type = dto.Type,
            Status = dto.Status,
            PermissionCode = permissionCode,
            IsVisible = dto.IsVisible,
            IsCache = dto.IsCache,
            IsAlwaysShow = dto.IsAlwaysShow
        };

        await _menuRepository.AddAsync(menu);
        var result = await GetByIdAsync(menu.Id);
        if (result.Data == null)
        {
            throw new InvalidOperationException("创建菜单失败");
        }
        return ApiResponseDto<MenuDto>.Success(result.Data, "创建成功");
    }

    public async Task<ApiResponseDto<MenuDto>> UpdateAsync(MenuUpdateDto dto)
    {
        var menu = await _menuRepository.GetByIdAsync(dto.Id);
        if (menu == null)
        {
            throw new InvalidOperationException("菜单不存在");
        }

        // 按钮类型：Code 和 PermissionCode 都存权限标识（用户只在权限标识输入框输入）
        string code;
        string permissionCode;
        if (dto.Type == 2)
        {
            // 按钮类型：优先使用 PermissionCode，如果没有则用 Code
            code = !string.IsNullOrEmpty(dto.PermissionCode) ? dto.PermissionCode : dto.Code;
            permissionCode = code;
        }
        else
        {
            code = dto.Code;
            permissionCode = dto.PermissionCode;
        }

        if (await _menuRepository.ExistsCodeAsync(code, dto.Id))
        {
            throw new InvalidOperationException($"菜单编码 {code} 已存在");
        }

        menu.ParentId = dto.ParentId;
        menu.Name = dto.Name;
        menu.Code = code;
        menu.Path = dto.Path;
        menu.Component = dto.Component;
        menu.Icon = dto.Icon;
        menu.Sort = dto.Sort;
        menu.Type = dto.Type;
        menu.Status = dto.Status;
        menu.PermissionCode = permissionCode;
        menu.IsVisible = dto.IsVisible;
        menu.IsCache = dto.IsCache;
        menu.IsAlwaysShow = dto.IsAlwaysShow;

        await _menuRepository.UpdateAsync(menu);
        var result = await GetByIdAsync(menu.Id);
        if (result.Data == null)
        {
            throw new InvalidOperationException("更新菜单失败");
        }
        return ApiResponseDto<MenuDto>.Success(result.Data, "更新成功");
    }

    public async Task<ApiResponseDto> DeleteAsync(long id)
    {
        await _menuRepository.DeleteAsync(id);
        return ApiResponseDto.Success(null, "删除成功");
    }

    private List<MenuDto> MapToTreeDto(List<Menu> menus)
    {
        return menus.Select(m => new MenuDto
        {
            Id = m.Id,
            ParentId = m.ParentId,
            Name = m.Name,
            Code = m.Code,
            Path = m.Path,
            Component = m.Component,
            Icon = m.Icon,
            Sort = m.Sort,
            Type = m.Type,
            Status = m.Status,
            PermissionCode = m.PermissionCode,
            IsVisible = m.IsVisible,
            IsCache = m.IsCache,
            IsAlwaysShow = m.IsAlwaysShow,
            Children = m.Children.Any() ? MapToTreeDto(m.Children.ToList()) : new List<MenuDto>()
        }).ToList();
    }
}
