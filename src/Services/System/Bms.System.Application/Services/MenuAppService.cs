using Mapster;
using Bms.System.Application.Dtos;
using Bms.System.Application.Dtos.Menus;
using Bms.System.Domain.Entities;
using Bms.System.Domain.IRepositories;
using Bms.System.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Bms.System.Application.Services;

public class MenuAppService : IMenuAppService
{
    private readonly IMenuRepository _menuRepository;
    private readonly SystemDbContext _context;

    public MenuAppService(IMenuRepository menuRepository, SystemDbContext context)
    {
        _menuRepository = menuRepository;
        _context = context;
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

    /// <summary>
    /// 获取当前用户在所有已授权子系统下的菜单树（全局搜索功能源数据源）
    /// 按「用户授权菜单 ∩ 各子系统关联菜单」取交集分组，未授权菜单的子系统不返回
    /// </summary>
    public async Task<ApiResponseDto<List<SubsystemMenusDto>>> GetAuthorizedAllAsync(long userId)
    {
        var menus = await _menuRepository.GetByUserIdAsync(userId);
        var authorizedMenuIds = menus.Select(m => m.Id).ToHashSet();

        // 已按授权菜单过滤的「菜单-子系统」关联
        var subsystemMenus = await _context.SubsystemMenus
            .Where(sm => authorizedMenuIds.Contains(sm.MenuId))
            .ToListAsync();
        var subsystems = await _context.Subsystems
            .Where(s => s.Status == 1)
            .OrderBy(s => s.Sort)
            .ToListAsync();

        var result = new List<SubsystemMenusDto>();
        foreach (var subsystem in subsystems)
        {
            var menuIdSet = subsystemMenus
                .Where(sm => sm.SubsystemId == subsystem.Id)
                .Select(sm => sm.MenuId)
                .ToHashSet();
            if (menuIdSet.Count == 0)
                continue;

            var subsystemMenuList = menus.Where(m => menuIdSet.Contains(m.Id)).ToList();
            result.Add(new SubsystemMenusDto
            {
                SubsystemId = subsystem.Id,
                SubsystemName = subsystem.Name,
                Menus = BuildTreeFromFlat(subsystemMenuList)
            });
        }

        return ApiResponseDto<List<SubsystemMenusDto>>.Success(result);
    }

    /// <summary>
    /// 将平铺菜单列表按 ParentId 组装为树
    /// （GetByUserIdAsync 返回平铺集合且 Children 导航未加载，MapToTreeDto 无法建树）
    /// </summary>
    private static List<MenuDto> BuildTreeFromFlat(List<Menu> menus)
    {
        MenuDto Map(Menu m) => new MenuDto
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
            Children = new List<MenuDto>()
        };

        var dtoMap = menus.ToDictionary(m => m.Id, Map);
        var roots = new List<MenuDto>();
        foreach (var m in menus.OrderBy(x => x.Sort))
        {
            if (m.ParentId.HasValue && dtoMap.TryGetValue(m.ParentId.Value, out var parent))
                parent.Children.Add(dtoMap[m.Id]);
            else
                roots.Add(dtoMap[m.Id]);
        }
        return roots;
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

        // 上级菜单校验：不能选择自身或自身下级菜单作为父级，否则形成循环引用导致树构建无限递归
        if (dto.ParentId is long parentId && parentId != 0)
        {
            if (parentId == dto.Id)
            {
                throw new InvalidOperationException("上级菜单不能选择自身");
            }
            var allMenus = await _menuRepository.GetListAsync();
            var menuDict = allMenus.ToDictionary(x => x.Id);
            var visited = new HashSet<long> { dto.Id };
            var cursor = parentId;
            while (cursor != 0)
            {
                if (cursor == dto.Id)
                {
                    throw new InvalidOperationException("上级菜单不能选择自身或自身的下级菜单");
                }
                if (!menuDict.TryGetValue(cursor, out var parent) || !visited.Add(cursor))
                {
                    break; // 父级不存在或数据中已存在循环引用，终止遍历
                }
                cursor = parent.ParentId ?? 0;
            }
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
