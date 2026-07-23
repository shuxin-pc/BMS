using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Bms.System.Application.Dtos;
using Bms.System.Application.Dtos.Roles;
using Bms.System.Application.Dtos.Menus;
using Bms.System.Application.Services;
using Bms.System.Domain.Attributes;
using Bms.System.Domain.Exceptions;

namespace Bms.System.Api.Controllers;

[ApiController]
[Route("api/system/[controller]")]
[Route("api/[controller]")]
[Authorize]
public class RolesController : ControllerBase
{
    private readonly IRoleAppService _roleService;
    private readonly IRoleMenuAuthAppService _roleMenuAuthAppService;

    public RolesController(
        IRoleAppService roleService,
        IRoleMenuAuthAppService roleMenuAuthAppService)
    {
        _roleService = roleService;
        _roleMenuAuthAppService = roleMenuAuthAppService;
    }

    /// <summary>
    /// 获取角色分页列表
    /// </summary>
    [HttpGet]
    public async Task<ApiResponseDto<PagedResponseDto<RoleDto>>> GetList([FromQuery] PagedRequestDto request)
    {
        // 获取当前用户信息进行租户隔离
        var (isSuperAdmin, tenantId) = GetCurrentUserInfo();
        return await _roleService.GetPagedListAsync(request, isSuperAdmin, tenantId);
    }

    /// <summary>
    /// 获取所有角色列表
    /// </summary>
    [HttpGet("all")]
    public async Task<ApiResponseDto<List<RoleDto>>> GetAll([FromQuery] long? tenantId)
    {
        // 获取当前用户信息进行租户隔离
        var (isSuperAdmin, currentTenantId) = GetCurrentUserInfo();
        // 超级管理员可以查询指定租户的角色，否则只能看到当前租户的角色
        long? effectiveTenantId = tenantId;
        if (!isSuperAdmin || !tenantId.HasValue)
        {
            effectiveTenantId = currentTenantId;
        }
        return await _roleService.GetAllListAsync(isSuperAdmin, effectiveTenantId);
    }

    /// <summary>
    /// 获取所有角色列表（不过滤租户，用于跨租户场景）
    /// 下拉查询辅助接口，仅需认证，不校验权限码
    /// </summary>
    [HttpGet("all-without-filter")]
    public async Task<ApiResponseDto<List<RoleDto>>> GetAllWithoutFilter()
    {
        return await _roleService.GetAllListWithoutFilterAsync();
    }

    /// <summary>
    /// 获取角色详情
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ApiResponseDto<RoleDto?>> GetById(long id)
    {
        return await _roleService.GetByIdAsync(id);
    }

    /// <summary>
    /// 创建角色
    /// </summary>
    [HttpPost]
    [Permission("system:role:add")]
    public async Task<ApiResponseDto<RoleDto>> Create([FromBody] RoleCreateDto dto)
    {
        try
        {
            // 获取当前用户的租户信息
            var tenantIdClaim = User.FindFirst("tenant_id");
            var tenantCodeClaim = User.FindFirst("tenant_code");
            var currentTenantId = tenantIdClaim != null ? long.Parse(tenantIdClaim.Value) : 1;
            var currentTenantCode = tenantCodeClaim?.Value ?? "platform";

            return await _roleService.CreateAsync(dto, currentTenantId, currentTenantCode);
        }
        catch (PermissionDeniedException ex)
        {
            return ApiResponseDto<RoleDto>.Fail(ex.Message, 403);
        }
        catch (InvalidOperationException ex)
        {
            return ApiResponseDto<RoleDto>.Fail(ex.Message, 400);
        }
    }

    /// <summary>
    /// 更新角色
    /// </summary>
    [HttpPut("{id}")]
    [Permission("system:role:edit")]
    public async Task<ApiResponseDto<RoleDto>> Update(long id, [FromBody] RoleUpdateDto dto)
    {
        try
        {
            dto.Id = id;
            return await _roleService.UpdateAsync(dto);
        }
        catch (PermissionDeniedException ex)
        {
            return ApiResponseDto<RoleDto>.Fail(ex.Message, 403);
        }
        catch (InvalidOperationException ex)
        {
            return ApiResponseDto<RoleDto>.Fail(ex.Message, 400);
        }
    }

    /// <summary>
    /// 删除角色
    /// </summary>
    [HttpDelete("{id}")]
    [Permission("system:role:delete")]
    public async Task<ApiResponseDto> Delete(long id)
    {
        try
        {
            return await _roleService.DeleteAsync(id);
        }
        catch (PermissionDeniedException ex)
        {
            return ApiResponseDto.Fail(ex.Message, 403);
        }
        catch (InvalidOperationException ex)
        {
            return ApiResponseDto.Fail(ex.Message, 400);
        }
    }

    /// <summary>
    /// 批量删除角色
    /// </summary>
    [HttpDelete("batch")]
    [Permission("system:role:delete")]
    public async Task<ApiResponseDto> BatchDelete([FromBody] BatchDeleteRequest request)
    {
        if (request?.Ids == null || request.Ids.Count == 0)
        {
            return ApiResponseDto.Fail("请选择要删除的角色", 400);
        }
        return await _roleService.BatchDeleteAsync(request.Ids);
    }

    /// <summary>
    /// 获取角色菜单权限
    /// </summary>
    [HttpGet("{id}/menus")]
    public async Task<ApiResponseDto<List<Application.Dtos.Menus.MenuDto>>> GetRoleMenus(long id)
    {
        return await _roleService.GetRoleMenusAsync(id);
    }

    /// <summary>
    /// 分配权限
    /// </summary>
    [HttpPost("{id}/permissions")]
    [Permission("system:role:edit")]
    public async Task<ApiResponseDto> AssignPermissions(long id, [FromBody] RoleAssignPermissionsDto dto)
    {
        try
        {
            return await _roleService.AssignPermissionsAsync(id, dto.PermissionIds);
        }
        catch (PermissionDeniedException ex)
        {
            return ApiResponseDto.Fail(ex.Message, 403);
        }
        catch (InvalidOperationException ex)
        {
            return ApiResponseDto.Fail(ex.Message, 400);
        }
    }

    /// <summary>
    /// 获取角色的菜单权限
    /// </summary>
    [HttpGet("{id}/menus/auth")]
    public async Task<ApiResponseDto<List<long>>> GetMenuAuths(long id)
    {
        return await _roleMenuAuthAppService.GetByRoleIdAsync(id);
    }

    /// <summary>
    /// 获取按子系统分组的菜单权限
    /// </summary>
    [HttpGet("{id}/menus/auth/grouped")]
    public async Task<ApiResponseDto<List<RoleMenuGroupedDto>>> GetMenuAuthsGrouped(long id)
    {
        var (isSuperAdmin, tenantId) = GetCurrentUserInfo();
        return await _roleMenuAuthAppService.GetGroupedByRoleIdAsync(id, tenantId, isSuperAdmin);
    }

    /// <summary>
    /// 更新角色的菜单权限（覆盖式，传空数组表示清空）
    /// </summary>
    [HttpPut("{id}/menus/auth")]
    [Permission("system:role:edit")]
    public async Task<ApiResponseDto> UpdateMenuAuths(long id, [FromBody] RoleMenuAssignDto dto)
    {
        try
        {
            return await _roleMenuAuthAppService.AssignMenusAsync(id, dto);
        }
        catch (PermissionDeniedException ex)
        {
            return ApiResponseDto.Fail(ex.Message, 403);
        }
        catch (InvalidOperationException ex)
        {
            return ApiResponseDto.Fail(ex.Message, 400);
        }
    }

    /// <summary>
    /// 删除角色的单个菜单权限
    /// </summary>
    [HttpDelete("{id}/menus/auth/{menuId}")]
    [Permission("system:role:delete")]
    public async Task<ApiResponseDto> RemoveMenuAuth(long id, long menuId)
    {
        try
        {
            return await _roleMenuAuthAppService.RemoveMenuAsync(id, menuId);
        }
        catch (PermissionDeniedException ex)
        {
            return ApiResponseDto.Fail(ex.Message, 403);
        }
        catch (InvalidOperationException ex)
        {
            return ApiResponseDto.Fail(ex.Message, 400);
        }
    }

    /// <summary>
    /// 获取当前用户信息
    /// </summary>
    private (bool isSuperAdmin, long? tenantId) GetCurrentUserInfo()
    {
        var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        var isSuperAdmin = roles.Contains("super_admin");
        var tenantIdClaim = User.FindFirst("tenant_id");
        var tenantId = tenantIdClaim != null ? long.Parse(tenantIdClaim.Value) : (long?)null;
        return (isSuperAdmin, tenantId);
    }
}