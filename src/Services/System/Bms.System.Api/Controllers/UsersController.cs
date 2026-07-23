using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Bms.System.Application.Dtos;
using Bms.System.Application.Dtos.Users;
using Bms.System.Application.Dtos.Profile;
using Bms.System.Application.Services;
using Bms.System.Domain.Attributes;
using Bms.System.Domain.Exceptions;
using Bms.System.Domain.Interfaces;
using Bms.System.Domain.Enums;

using RoleDto = Bms.System.Application.Dtos.Users.UserRoleDto;

namespace Bms.System.Api.Controllers;

[ApiController]
[Route("api/system/[controller]")]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserAppService _userService;
    private readonly IDataPermissionFilter _dataPermissionFilter;

    public UsersController(IUserAppService userService, IDataPermissionFilter dataPermissionFilter)
    {
        _userService = userService;
        _dataPermissionFilter = dataPermissionFilter;
    }

    /// <summary>
    /// 获取用户分页列表
    /// </summary>
    [HttpGet]
    public async Task<ApiResponseDto<PagedResponseDto<UserDto>>> GetList([FromQuery] PagedRequestDto request)
    {
        // 获取当前用户角色
        var currentUserRoles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        var isSuperAdmin = currentUserRoles.Contains("super_admin");
        var isTenantAdmin = currentUserRoles.Contains("tenant_admin");
        var tenantIdClaim = User.FindFirst("tenant_id");
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

        // 租户隔离：非超级管理员只能看到当前租户的用户
        if (!isSuperAdmin && tenantIdClaim != null && long.TryParse(tenantIdClaim.Value, out long currentTenantId))
        {
            // 如果请求中没有指定租户筛选条件，默认使用当前租户
            if (!request.TenantId.HasValue)
            {
                request.TenantId = currentTenantId;
            }
            // tenant_admin 自身由 super_admin 创建（CreatorTenantId=平台租户），不应被 CreatorTenantId 过滤掉
            // 普通用户设置 CreatorTenantId 过滤：屏蔽平台跨租户创建的用户（含 tenant_admin）
            if (!isTenantAdmin)
            {
                request.CreatorTenantId = currentTenantId;
            }
        }

        // 数据权限过滤：仅普通用户需要按组织过滤
        // tenant_admin 数据权限范围为全租户（DataScopeType=All），无需按组织过滤
        if (!isSuperAdmin && !isTenantAdmin && userIdClaim != null && long.TryParse(userIdClaim.Value, out long currentUserId))
        {
            // 获取当前用户的数据权限范围
            var scope = await _dataPermissionFilter.GetDataPermissionScopeAsync(currentUserId);

            // 根据数据权限类型设置组织ID过滤
            if (scope.ScopeType == DataScopeType.Self)
            {
                // 仅本人：只能看到自己
                request.UserId = currentUserId;
            }
            else if (scope.OrganizationIds.Any())
            {
                // 部门及以下/自定义：按组织ID列表过滤
                request.OrganizationIds = scope.OrganizationIds;
            }
            // 全部数据：不做额外过滤（已有租户隔离）
        }

        return await _userService.GetPagedListAsync(request);
    }

    /// <summary>
    /// 获取用户列表（带租户隔离和姓名筛选）
    /// </summary>
    [HttpGet("all")]
    public async Task<ApiResponseDto<List<UserDto>>> GetAll([FromQuery] long? tenantId, [FromQuery] string? realName)
    {
        // 获取当前用户角色
        var currentUserRoles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        var isSuperAdmin = currentUserRoles.Contains("super_admin");
        var tenantIdClaim = User.FindFirst("tenant_id");

        // 租户隔离：非超级管理员只能看到当前租户的用户
        long? effectiveTenantId = tenantId;
        if (!isSuperAdmin && tenantIdClaim != null && long.TryParse(tenantIdClaim.Value, out long currentTenantId))
        {
            effectiveTenantId = currentTenantId;
        }

        return await _userService.GetAllListAsync(effectiveTenantId, realName);
    }

    /// <summary>
    /// 获取用户详情
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ApiResponseDto<UserDto?>> GetById(long id)
    {
        return await _userService.GetByIdAsync(id);
    }

    /// <summary>
    /// 创建用户
    /// </summary>
    [HttpPost]
    [Permission("system:user:add")]
    public async Task<ApiResponseDto<UserDto>> Create([FromBody] UserCreateDto dto)
    {
        try
        {
            // 获取当前用户的角色信息
            var currentUserRoles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
            var tenantIdClaim = User.FindFirst("tenant_id");
            var tenantCodeClaim = User.FindFirst("tenant_code");

            // 构建创建上下文
            var createContext = new UserCreateContext
            {
                CurrentUserRoles = currentUserRoles,
                CurrentTenantId = tenantIdClaim != null ? long.Parse(tenantIdClaim.Value) : 0,
                CurrentTenantCode = tenantCodeClaim?.Value ?? string.Empty
            };

            return await _userService.CreateAsync(dto, createContext);
        }
        catch (PermissionDeniedException ex)
        {
            return ApiResponseDto<UserDto>.Fail(ex.Message, 403);
        }
        catch (InvalidOperationException ex)
        {
            return ApiResponseDto<UserDto>.Fail(ex.Message, 400);
        }
    }

    /// <summary>
    /// 更新用户
    /// </summary>
    [HttpPut("{id}")]
    [Permission("system:user:edit")]
    public async Task<ApiResponseDto<UserDto>> Update(long id, [FromBody] UserUpdateDto dto)
    {
        try
        {
            dto.Id = id;
            return await _userService.UpdateAsync(dto);
        }
        catch (PermissionDeniedException ex)
        {
            return ApiResponseDto<UserDto>.Fail(ex.Message, 403);
        }
        catch (InvalidOperationException ex)
        {
            return ApiResponseDto<UserDto>.Fail(ex.Message, 400);
        }
    }

    /// <summary>
    /// 删除用户
    /// </summary>
    [HttpDelete("{id}")]
    [Permission("system:user:delete")]
    public async Task<ApiResponseDto> Delete(long id)
    {
        try
        {
            return await _userService.DeleteAsync(id);
        }
        catch (PermissionDeniedException ex)
        {
            return ApiResponseDto.Fail(ex.Message, 403);
        }
    }

    /// <summary>
    /// 批量删除用户
    /// </summary>
    [HttpDelete("batch")]
    [Permission("system:user:delete")]
    public async Task<ApiResponseDto> BatchDelete([FromBody] BatchDeleteRequest request)
    {
        if (request?.Ids == null || request.Ids.Count == 0)
        {
            return ApiResponseDto.Fail("请选择要删除的用户", 400);
        }
        try
        {
            return await _userService.BatchDeleteAsync(request.Ids);
        }
        catch (PermissionDeniedException ex)
        {
            return ApiResponseDto.Fail(ex.Message, 403);
        }
    }

    /// <summary>
    /// 重置密码
    /// </summary>
    [HttpPost("{id}/reset-password")]
    [Permission("system:user:resetPwd")]
    public async Task<ApiResponseDto> ResetPassword(long id, [FromBody] UserPasswordDto dto)
    {
        try
        {
            return await _userService.ResetPasswordAsync(id, dto.NewPassword);
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
    /// 修改密码（当前用户自助，无需权限码，但需登录）
    /// </summary>
    [HttpPost("change-password")]
    public async Task<ApiResponseDto> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        try
        {
            // 从 Claims 获取当前用户ID（修复原 userId = 1L 硬编码漏洞）
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                            ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
            {
                return ApiResponseDto.Fail("无法识别当前用户身份", 401);
            }
            return await _userService.ChangePasswordAsync(userId, dto.OldPassword, dto.NewPassword);
        }
        catch (InvalidOperationException ex)
        {
            return ApiResponseDto.Fail(ex.Message, 400);
        }
    }

    /// <summary>
    /// 分配角色
    /// </summary>
    [HttpPost("{id}/assign-roles")]
    [Permission("system:user:edit")]
    public async Task<ApiResponseDto> AssignRoles(long id, [FromBody] UserAssignRolesDto dto)
    {
        try
        {
            return await _userService.AssignRolesAsync(id, dto.RoleIds);
        }
        catch (PermissionDeniedException ex)
        {
            return ApiResponseDto.Fail(ex.Message, 403);
        }
    }

    /// <summary>
    /// 获取用户角色
    /// </summary>
    [HttpGet("{id}/roles")]
    public async Task<ApiResponseDto<List<UserRoleDto>>> GetUserRoles(long id)
    {
        return await _userService.GetUserRolesAsync(id);
    }
}