using Bms.BuildingBlocks.Abstractions.Security;
using Bms.System.Application.Dtos;
using Bms.System.Application.Dtos.Menus;
using Bms.System.Application.Dtos.Roles;
using Bms.System.Domain.Entities;
using Bms.System.Domain.Exceptions;
using Bms.System.Domain.Interfaces;
using Bms.System.Domain.IRepositories;

namespace Bms.System.Application.Services;

/// <summary>
/// 角色菜单权限应用服务实现
/// </summary>
public class RoleMenuAuthAppService : IRoleMenuAuthAppService
{
    private readonly IRoleMenuAuthRepository _roleMenuAuthRepository;
    private readonly ISubsystemMenuRepository _subsystemMenuRepository;
    private readonly ISubsystemRepository _subsystemRepository;
    private readonly IMenuRepository _menuRepository;
    private readonly ITenantSubsystemRepository _tenantSubsystemRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IUserPermissionChecker _userPermissionChecker;

    // 受保护角色 Code：仅 super_admin 可修改其菜单权限
    private static readonly HashSet<string> ProtectedRoleCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "super_admin",
        "tenant_admin"
    };

    public RoleMenuAuthAppService(
        IRoleMenuAuthRepository roleMenuAuthRepository,
        ISubsystemMenuRepository subsystemMenuRepository,
        ISubsystemRepository subsystemRepository,
        IMenuRepository menuRepository,
        ITenantSubsystemRepository tenantSubsystemRepository,
        IRoleRepository roleRepository,
        ICurrentUser currentUser,
        IUserPermissionChecker userPermissionChecker)
    {
        _roleMenuAuthRepository = roleMenuAuthRepository;
        _subsystemMenuRepository = subsystemMenuRepository;
        _subsystemRepository = subsystemRepository;
        _menuRepository = menuRepository;
        _tenantSubsystemRepository = tenantSubsystemRepository;
        _roleRepository = roleRepository;
        _currentUser = currentUser;
        _userPermissionChecker = userPermissionChecker;
    }

    /// <summary>
    /// 校验当前用户能否修改目标角色的菜单权限
    /// 所有非 super_admin 角色一视同仁：Controller 层权限码校验通过后，
    /// 此处仅校验 ProtectedCode 保护 + Level 约束 + 租户隔离
    /// </summary>
    private async Task CheckCanModifyRoleMenuAuthAsync(long roleId)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId == null)
        {
            throw new PermissionDeniedException("无法识别当前用户身份");
        }

        var ctx = await _userPermissionChecker.GetContextAsync(_currentUser.UserId.Value);
        if (ctx.IsSuperAdmin)
        {
            return;
        }

        var targetRole = await _roleRepository.GetByIdAsync(roleId);
        if (targetRole == null)
        {
            throw new InvalidOperationException("目标角色不存在");
        }

        // ProtectedCode 保护
        if (ProtectedRoleCodes.Contains(targetRole.Code))
        {
            throw new PermissionDeniedException("无权修改系统保留角色的菜单权限");
        }

        // Level 约束：目标角色 Level 必须 > 当前用户 MaxRoleLevel
        if (targetRole.Level <= ctx.MaxRoleLevel)
        {
            throw new PermissionDeniedException("无权操作同级或更高级别的角色");
        }

        // 租户隔离
        var currentTenantId = _currentUser.TenantId ?? 0;
        if (targetRole.TenantId != currentTenantId)
        {
            throw new PermissionDeniedException("无权修改其他租户角色的菜单权限");
        }
    }

    /// <summary>
    /// 校验当前用户能否查询目标角色的菜单权限配置
    /// 修复 H1：角色详情与菜单权限信息泄露
    /// 查询场景校验：super_admin 放行 + ProtectedCode 保护 + 租户隔离
    /// </summary>
    private async Task CheckCanReadRoleMenuAuthAsync(long roleId)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId == null)
        {
            throw new PermissionDeniedException("无法识别当前用户身份");
        }

        var ctx = await _userPermissionChecker.GetContextAsync(_currentUser.UserId.Value);
        if (ctx.IsSuperAdmin)
        {
            return;
        }

        // 允许查看自己的角色：目标角色属于当前用户已拥有的角色集合时放行
        if (ctx.RoleIds.Contains(roleId))
        {
            return;
        }

        var targetRole = await _roleRepository.GetByIdAsync(roleId);
        if (targetRole == null)
        {
            throw new InvalidOperationException("目标角色不存在");
        }

        // ProtectedCode 保护：非 super_admin 不得查看系统保留角色的菜单权限配置
        if (ProtectedRoleCodes.Contains(targetRole.Code))
        {
            throw new PermissionDeniedException("无权查看系统保留角色的菜单权限");
        }

        // 租户隔离
        var currentTenantId = _currentUser.TenantId ?? 0;
        if (targetRole.TenantId != currentTenantId)
        {
            throw new PermissionDeniedException("无权查看其他租户角色的菜单权限");
        }
    }

    /// <summary>
    /// 校验当前用户能否为角色分配指定菜单（修复 S3：权限不放大原则）
    /// 原 AssignMenusAsync 只校验目标角色，未校验 dto.MenuIds 是否在操作者自身菜单范围内，
    /// 导致操作者可给下级角色分配自己没有的菜单权限，形成权限放大。
    /// 规则：
    /// - super_admin：放行
    /// - 其他用户：menuIds 必须是操作者自身所有角色已拥有菜单 ID 集合的子集
    /// </summary>
    private async Task CheckCanAssignMenusAsync(List<long> menuIds)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId == null)
        {
            throw new PermissionDeniedException("无法识别当前用户身份");
        }

        var ctx = await _userPermissionChecker.GetContextAsync(_currentUser.UserId.Value);
        if (ctx.IsSuperAdmin)
        {
            return;
        }

        if (menuIds == null || !menuIds.Any())
        {
            return;
        }

        // 查询操作者自身所有角色已拥有的菜单 ID 集合
        var ownRoles = await _roleRepository.GetByUserIdAsync(_currentUser.UserId.Value);
        var ownRoleIds = ownRoles.Select(r => r.Id).ToList();
        var ownMenuAuths = await _roleMenuAuthRepository.GetByRoleIdsAsync(ownRoleIds);
        var ownMenuIds = ownMenuAuths.Select(x => x.MenuId).ToHashSet();

        var unauthorizedIds = menuIds.Distinct().Where(id => !ownMenuIds.Contains(id)).ToList();
        if (unauthorizedIds.Any())
        {
            // 查询菜单名称，用于在错误信息中直观展示超出权限的菜单
            var allMenus = await _menuRepository.GetListAsync();
            var menuDict = allMenus.ToDictionary(x => x.Id);
            var unauthorizedNames = unauthorizedIds
                .Select(id => menuDict.TryGetValue(id, out var m) ? m.Name : $"[未知菜单:{id}]")
                .ToList();
            throw new PermissionDeniedException($"无权分配以下菜单（超出自身菜单权限范围）：{string.Join(",", unauthorizedNames)}");
        }
    }

    public async Task<ApiResponseDto<List<long>>> GetByRoleIdAsync(long roleId)
    {
        // 修复 H1：校验当前用户能否查询此角色的菜单权限（租户隔离 + ProtectedCode 保护）
        await CheckCanReadRoleMenuAuthAsync(roleId);

        var roleMenuAuths = await _roleMenuAuthRepository.GetByRoleIdAsync(roleId);
        var menuIds = roleMenuAuths.Select(x => x.MenuId).ToList();
        return ApiResponseDto<List<long>>.Success(menuIds);
    }

    public async Task<ApiResponseDto<List<RoleMenuGroupedDto>>> GetGroupedByRoleIdAsync(long roleId, long? tenantId = null, bool isSuperAdmin = false)
    {
        // 修复 H1：校验当前用户能否查询此角色的菜单权限（租户隔离 + ProtectedCode 保护）
        await CheckCanReadRoleMenuAuthAsync(roleId);

        var roleMenuAuths = await _roleMenuAuthRepository.GetByRoleIdAsync(roleId);
        var selectedMenuIds = roleMenuAuths.Select(x => x.MenuId).ToList();

        // 非超级管理员：获取当前用户的管理员角色所授权的菜单ID
        HashSet<long>? allowedMenuIds = null;
        if (!isSuperAdmin && tenantId.HasValue)
        {
            // 获取管理员角色的菜单权限
            var tenantAdminRole = await _roleRepository.GetTenantAdminRoleAsync(tenantId.Value);
            if (tenantAdminRole != null)
            {
                var tenantAdminMenuAuths = await _roleMenuAuthRepository.GetByRoleIdAsync(tenantAdminRole.Id);
                allowedMenuIds = tenantAdminMenuAuths.Select(x => x.MenuId).ToHashSet();
            }
        }

        // 获取子系统列表，根据租户进行过滤
        List<Subsystem> subsystems;
        if (tenantId.HasValue)
        {
            // 获取租户分配的子系统
            var tenantSubsystems = await _tenantSubsystemRepository.GetByTenantIdAsync(tenantId.Value);
            var tenantSubsystemIds = tenantSubsystems.Select(x => x.SubsystemId).ToList();
            var allSubsystems = await _subsystemRepository.GetListAsync();
            subsystems = allSubsystems.Where(x => tenantSubsystemIds.Contains(x.Id)).OrderBy(x => x.Sort).ToList();
        }
        else
        {
            // 超级管理员返回所有子系统
            subsystems = (await _subsystemRepository.GetListAsync()).OrderBy(x => x.Sort).ToList();
        }

        var allMenus = await _menuRepository.GetListAsync();
        var menuDict = allMenus.ToDictionary(x => x.Id);

        var subsystemMenusDict = (await _subsystemMenuRepository.GetByMenuIdsAsync(allMenus.Select(x => x.Id)))
            .GroupBy(x => x.SubsystemId)
            .ToDictionary(x => x.Key, x => x.Select(y => y.MenuId).ToList());

        var groupedMenus = allMenus.Where(x => x.Type != 2).ToList();

        var result = new List<RoleMenuGroupedDto>();

        foreach (var subsystem in subsystems)
        {
            var menuIdsInSubsystem = subsystemMenusDict.GetValueOrDefault(subsystem.Id) ?? new List<long>();

            // 扩展为包含所有后代菜单的完整列表
            var menuIdsWithDescendants = new HashSet<long>(menuIdsInSubsystem);
            foreach (var menuId in menuIdsInSubsystem)
            {
                CollectDescendantMenuIds(menuId, menuDict, menuIdsWithDescendants);
            }

            // 检查该子系统是否有可分配的菜单（SubsystemMenus中存储的勾选菜单）
            if (!menuIdsWithDescendants.Any())
            {
                continue;
            }

            // 非超级管理员：只显示管理员角色授权的菜单及其父级
            HashSet<long> allowedSet;
            if (allowedMenuIds != null)
            {
                // 取管理员授权菜单与当前子系统菜单的交集
                allowedSet = menuIdsWithDescendants.Intersect(allowedMenuIds).ToHashSet();
                if (!allowedSet.Any())
                {
                    continue; // 如果该子系统没有可授权的菜单，跳过
                }
            }
            else
            {
                // 超级管理员：使用子系统所有可分配的菜单
                allowedSet = menuIdsWithDescendants;
            }

            var menuTree = BuildMenuTreeWithParentFilter(allMenus, allowedSet, 0);
            //var selectedMenuIdsInSubsystem = selectedMenuIds.Intersect(menuIdsWithDescendants).ToList();

            var dto = new RoleMenuGroupedDto
            {
                SubsystemId = subsystem.Id,
                SubsystemCode = subsystem.Code,
                SubsystemName = subsystem.Name,
                SubsystemIcon = subsystem.Icon,
                Menus = menuTree,
                SelectedMenuIds = selectedMenuIds
            };

            result.Add(dto);
        }

        return ApiResponseDto<List<RoleMenuGroupedDto>>.Success(result);
    }

    public async Task<ApiResponseDto> AssignMenusAsync(long roleId, RoleMenuAssignDto dto)
    {
        // 权限校验：目标角色租户隔离 + 系统保留角色保护
        await CheckCanModifyRoleMenuAuthAsync(roleId);
        // 权限校验：分配的菜单不得超出操作者自身菜单权限范围（修复 S3 权限放大）
        await CheckCanAssignMenusAsync(dto.MenuIds);

        var existingRoleMenuAuths = await _roleMenuAuthRepository.GetByRoleIdAsync(roleId);
        var existingMenuIds = existingRoleMenuAuths.Select(x => x.MenuId).ToList();
        var newMenuIds = dto.MenuIds.Distinct().ToList();

        var toRemove = existingMenuIds.Except(newMenuIds).ToList();
        var toAdd = newMenuIds.Except(existingMenuIds).ToList();

        foreach (var menuId in toRemove)
        {
            await _roleMenuAuthRepository.DeleteByRoleIdAndMenuIdAsync(roleId, menuId);
        }

        foreach (var menuId in toAdd)
        {
            // 查找该菜单所属的子系统ID
            long? subsystemId = await GetSubsystemIdForMenuAsync(menuId);
            if (!subsystemId.HasValue)
            {
                // 如果找不到，尝试通过后代菜单查找子系统
                subsystemId = await GetSubsystemIdFromDescendantMenusAsync(menuId);
            }
            if (!subsystemId.HasValue)
            {
                continue; // 完全找不到所属子系统，跳过
            }

            var roleMenuAuth = new RoleMenuAuth
            {
                RoleId = roleId,
                MenuId = menuId,
                SubsystemId = subsystemId.Value,
                CreatedTime = DateTime.Now,
                UpdatedTime = DateTime.Now
            };
            await _roleMenuAuthRepository.AddRangeAsync(new[] { roleMenuAuth });
        }

        return ApiResponseDto.Success(null, "分配成功");
    }

    /// <summary>
    /// 获取菜单所属的子系统ID（优先从SubsystemMenus查找，没有则通过父级菜单查找）
    /// </summary>
    private async Task<long?> GetSubsystemIdForMenuAsync(long menuId)
    {
        // 1. 先尝试直接从 SubsystemMenus 查找
        var subsystemMenus = await _subsystemMenuRepository.GetByMenuIdAsync(menuId);
        if (subsystemMenus.Any())
        {
            return subsystemMenus.First().SubsystemId;
        }

        // 2. 如果找不到，尝试通过父级菜单查找
        var menu = await _menuRepository.GetByIdAsync(menuId);
        if (menu != null && menu.ParentId.HasValue && menu.ParentId != 0)
        {
            // 递归查找父级菜单的子系统
            return await GetSubsystemIdForMenuAsync(menu.ParentId.Value);
        }

        return null;
    }

    /// <summary>
    /// 通过后代菜单查找所属的子系统ID
    /// 如果某个后代菜单已分配子系统，则该菜单也默认包含该子系统
    /// </summary>
    private async Task<long?> GetSubsystemIdFromDescendantMenusAsync(long parentMenuId)
    {
        // 获取所有菜单用于构建树结构
        var allMenus = await _menuRepository.GetListAsync();
        var menuDict = allMenus.ToDictionary(x => x.Id);

        // 收集所有后代菜单ID
        var descendantIds = GetAllDescendantMenuIds(parentMenuId, menuDict);

        if (!descendantIds.Any())
        {
            return null;
        }

        // 批量查询后代菜单的子系统
        var subsystemMenus = await _subsystemMenuRepository.GetByMenuIdsAsync(descendantIds);
        return subsystemMenus.FirstOrDefault()?.SubsystemId;
    }

    /// <summary>
    /// 获取指定菜单的所有后代菜单ID
    /// </summary>
    private HashSet<long> GetAllDescendantMenuIds(long parentMenuId, Dictionary<long, Menu> menuDict)
    {
        var result = new HashSet<long>();
        CollectDescendantIds(parentMenuId, menuDict, result);
        return result;
    }

    /// <summary>
    /// 递归收集后代菜单ID
    /// </summary>
    private void CollectDescendantIds(long parentMenuId, Dictionary<long, Menu> menuDict, HashSet<long> result)
    {
        var children = menuDict.Values.Where(x => x.ParentId == parentMenuId);
        foreach (var child in children)
        {
            if (result.Add(child.Id))
            {
                CollectDescendantIds(child.Id, menuDict, result);
            }
        }
    }

    public async Task<ApiResponseDto> RemoveMenuAsync(long roleId, long menuId)
    {
        // 权限校验：目标角色租户隔离 + 系统保留角色保护
        await CheckCanModifyRoleMenuAuthAsync(roleId);

        await _roleMenuAuthRepository.DeleteByRoleIdAndMenuIdAsync(roleId, menuId);
        return ApiResponseDto.Success(null, "移除成功");
    }

    private List<MenuDto> BuildMenuTree(List<Menu> menus, long parentId)
    {
        var result = new List<MenuDto>();
        var children = menus.Where(x => x.ParentId == parentId).ToList();

        foreach (var child in children.OrderBy(x => x.Sort))
        {
            var dto = new MenuDto
            {
                Id = child.Id,
                ParentId = child.ParentId,
                Name = child.Name,
                Code = child.Code,
                Path = child.Path,
                Component = child.Component,
                Icon = child.Icon,
                Sort = child.Sort,
                Type = child.Type,
                Status = child.Status,
                PermissionCode = child.PermissionCode,
                IsVisible = child.IsVisible,
                IsCache = child.IsCache,
                IsAlwaysShow = child.IsAlwaysShow,
                Children = BuildMenuTree(menus, child.Id)
            };
            result.Add(dto);
        }

        return result;
    }

    /// <summary>
    /// 构建菜单树，但只包含指定菜单及其祖先（用于处理SubsystemMenus只存子菜单的情况）
    /// </summary>
    private List<MenuDto> BuildMenuTreeWithParentFilter(List<Menu> allMenus, HashSet<long> allowedMenuIds, long parentId)
    {
        var result = new List<MenuDto>();

        // 找到所有需要显示的菜单（allowedMenuIds 中的菜单 + 它们的祖先）
        var visibleMenus = GetVisibleMenusWithAncestors(allMenus, allowedMenuIds);
        var visibleMenuIds = visibleMenus.Select(x => x.Id).ToHashSet();

        // 从可见菜单中找子菜单，但递归时包含所有后代菜单
        // 注意：根菜单的ParentId为null或0，需要特殊处理
        var children = visibleMenus
            .Where(x => parentId == 0
                ? (x.ParentId == null || x.ParentId == 0)
                : x.ParentId == parentId)
            .OrderBy(x => x.Sort)
            .ToList();

        foreach (var child in children)
        {
            var dto = new MenuDto
            {
                Id = child.Id,
                ParentId = child.ParentId,
                Name = child.Name,
                Code = child.Code,
                Path = child.Path,
                Component = child.Component,
                Icon = child.Icon,
                Sort = child.Sort,
                Type = child.Type,
                Status = child.Status,
                PermissionCode = child.PermissionCode,
                IsVisible = child.IsVisible,
                IsCache = child.IsCache,
                IsAlwaysShow = child.IsAlwaysShow,
                // 递归构建子菜单时，只包含 visibleMenuIds 中的菜单
                Children = BuildMenuTreeWithDescendants(allMenus, visibleMenuIds, allowedMenuIds, child.Id)
            };
            result.Add(dto);
        }

        return result;
    }

    /// <summary>
    /// 构建指定菜单的后代菜单树，只包含 visibleMenuIds 中的菜单及其后代
    /// </summary>
    private List<MenuDto> BuildMenuTreeWithDescendants(List<Menu> allMenus, HashSet<long> visibleMenuIds, HashSet<long> allowedMenuIds, long parentId)
    {
        var result = new List<MenuDto>();
        // 处理根菜单的情况：parentId=0 时需要同时匹配 ParentId=null 和 ParentId=0
        var children = parentId == 0
            ? allMenus.Where(x => x.ParentId == null || x.ParentId == 0).OrderBy(x => x.Sort).ToList()
            : allMenus.Where(x => x.ParentId == parentId).OrderBy(x => x.Sort).ToList();

        foreach (var child in children)
        {
            // 只处理 visibleMenuIds 中存在的菜单（允许显示的菜单 = 子系统分配的菜单及其祖先）
            if (!visibleMenuIds.Contains(child.Id))
            {
                continue;
            }

            var dto = new MenuDto
            {
                Id = child.Id,
                ParentId = child.ParentId,
                Name = child.Name,
                Code = child.Code,
                Path = child.Path,
                Component = child.Component,
                Icon = child.Icon,
                Sort = child.Sort,
                Type = child.Type,
                Status = child.Status,
                PermissionCode = child.PermissionCode,
                IsVisible = child.IsVisible,
                IsCache = child.IsCache,
                IsAlwaysShow = child.IsAlwaysShow,
                Children = BuildMenuTreeWithDescendants(allMenus, visibleMenuIds, allowedMenuIds, child.Id)
            };
            result.Add(dto);
        }

        return result;
    }

    /// <summary>
    /// 获取需要显示的菜单集合（包括指定菜单及其所有祖先）
    /// </summary>
    private List<Menu> GetVisibleMenusWithAncestors(List<Menu> allMenus, HashSet<long> allowedMenuIds)
    {
        var result = new HashSet<long>();
        var menuDict = allMenus.ToDictionary(x => x.Id);

        // 从允许的菜单开始，向上遍历找所有祖先
        foreach (var menuId in allowedMenuIds)
        {
            CollectAncestors(menuId, menuDict, result);
        }

        return allMenus.Where(x => result.Contains(x.Id)).ToList();
    }

    /// <summary>
    /// 递归收集菜单及其所有后代
    /// </summary>
    private void CollectDescendantMenuIds(long parentId, Dictionary<long, Menu> menuDict, HashSet<long> result)
    {
        // 处理根菜单的情况：parentId=0 时需要同时匹配 ParentId=null 和 ParentId=0
        var children = parentId == 0
            ? menuDict.Values.Where(x => x.ParentId == null || x.ParentId == 0)
            : menuDict.Values.Where(x => x.ParentId == parentId);

        foreach (var child in children)
        {
            if (result.Add(child.Id))
            {
                CollectDescendantMenuIds(child.Id, menuDict, result);
            }
        }
    }

    /// <summary>
    /// 递归收集菜单及其所有祖先
    /// </summary>
    private void CollectAncestors(long menuId, Dictionary<long, Menu> menuDict, HashSet<long> result)
    {
        if (result.Contains(menuId))
        {
            return;
        }

        if (menuDict.TryGetValue(menuId, out var menu))
        {
            result.Add(menuId);
            if (menu.ParentId.HasValue && menu.ParentId != 0)
            {
                CollectAncestors(menu.ParentId.Value, menuDict, result);
            }
        }
    }
}
