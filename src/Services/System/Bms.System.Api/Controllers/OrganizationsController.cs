using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Bms.System.Application.Dtos;
using Bms.System.Application.Dtos.Organizations;
using Bms.System.Application.Services;
using Bms.BuildingBlocks.Abstractions.Security;
using Bms.System.Domain.Exceptions;

namespace Bms.System.Api.Controllers;

[ApiController]
[Route("api/system/[controller]")]
[Route("api/[controller]")]
[Authorize]
public class OrganizationsController : ControllerBase
{
    private readonly IOrganizationAppService _organizationService;

    public OrganizationsController(IOrganizationAppService organizationService)
    {
        _organizationService = organizationService;
    }

    /// <summary>
    /// 获取组织树形列表
    /// </summary>
    [HttpGet("tree")]
    [Permission("system:organization:view")]
    public async Task<ApiResponseDto<List<OrganizationDto>>> GetTree([FromQuery] OrganizationQueryDto? query)
    {
        // 获取当前用户信息进行租户隔离
        var (isSuperAdmin, tenantId) = GetCurrentUserInfo();

        // 租户隔离：非超级管理员强制使用当前租户，忽略 query.TenantId 防止跨租户越权
        // 仅超级管理员可通过 query.TenantId 跨租户查询
        long? effectiveTenantId = null;
        if (!isSuperAdmin && tenantId.HasValue)
        {
            // 非超级管理员：强制使用当前租户，忽略前端传入的 tenantId 防止越权
            effectiveTenantId = tenantId;
        }
        else if (isSuperAdmin && query?.TenantId.HasValue == true)
        {
            // 超级管理员：按传入的 tenantId 筛选
            effectiveTenantId = query.TenantId;
        }

        var result = await _organizationService.GetTreeListAsync(query, isSuperAdmin, effectiveTenantId);
        return ApiResponseDto<List<OrganizationDto>>.Success(result);
    }

    /// <summary>
    /// 获取组织树（下拉数据专用）
    /// 下拉查询辅助接口，仅需认证，不校验权限码
    /// 用于用户管理、角色管理、站内信等页面的组织下拉选择，避免因未分配组织架构页面权限导致下拉无选项
    /// 租户隔离：非超级管理员强制使用当前租户，仅超级管理员可通过 query.TenantId 跨租户查询
    /// </summary>
    [HttpGet("options")]
    public async Task<ApiResponseDto<List<OrganizationDto>>> GetOptions([FromQuery] OrganizationQueryDto? query)
    {
        // 获取当前用户信息进行租户隔离
        var (isSuperAdmin, tenantId) = GetCurrentUserInfo();

        // 租户隔离：非超级管理员强制使用当前租户，忽略 query.TenantId 防止跨租户越权
        // 仅超级管理员可通过 query.TenantId 跨租户查询
        long? effectiveTenantId = null;
        if (!isSuperAdmin && tenantId.HasValue)
        {
            // 非超级管理员：强制使用当前租户，忽略前端传入的 tenantId 防止越权
            effectiveTenantId = tenantId;
        }
        else if (isSuperAdmin && query?.TenantId.HasValue == true)
        {
            // 超级管理员：按传入的 tenantId 筛选
            effectiveTenantId = query.TenantId;
        }

        var result = await _organizationService.GetTreeListAsync(query, isSuperAdmin, effectiveTenantId);
        return ApiResponseDto<List<OrganizationDto>>.Success(result);
    }

    /// <summary>
    /// 获取组织列表
    /// </summary>
    [HttpGet]
    [Permission("system:organization:view")]
    public async Task<ApiResponseDto<List<OrganizationDto>>> GetList([FromQuery] OrganizationQueryDto query)
    {
        // 获取当前用户信息进行租户隔离
        var (isSuperAdmin, tenantId) = GetCurrentUserInfo();

        // 租户隔离：非超级管理员强制使用当前租户，忽略 query.TenantId 防止跨租户越权
        // 仅超级管理员可通过 query.TenantId 跨租户查询
        long? effectiveTenantId = null;
        if (!isSuperAdmin && tenantId.HasValue)
        {
            // 非超级管理员：强制使用当前租户，忽略前端传入的 tenantId 防止越权
            effectiveTenantId = tenantId;
        }
        else if (isSuperAdmin && query.TenantId.HasValue)
        {
            // 超级管理员：按传入的 tenantId 筛选
            effectiveTenantId = query.TenantId;
        }

        var result = await _organizationService.GetListAsync(query, isSuperAdmin, effectiveTenantId);
        return ApiResponseDto<List<OrganizationDto>>.Success(result);
    }

    /// <summary>
    /// 获取组织详情
    /// </summary>
    [HttpGet("{id}")]
    [Permission("system:organization:view")]
    public async Task<ApiResponseDto<OrganizationDto>> GetById(long id)
    {
        var result = await _organizationService.GetByIdAsync(id);
        return ApiResponseDto<OrganizationDto>.Success(result);
    }

    /// <summary>
    /// 获取子组织列表
    /// </summary>
    [HttpGet("{parentId}/children")]
    [Permission("system:organization:view")]
    public async Task<ApiResponseDto<List<OrganizationDto>>> GetChildren(long parentId)
    {
        var result = await _organizationService.GetChildrenAsync(parentId);
        return ApiResponseDto<List<OrganizationDto>>.Success(result);
    }

    /// <summary>
    /// 创建组织
    /// </summary>
    [HttpPost]
    [Permission("system:organization:add")]
    public async Task<ApiResponseDto<OrganizationDto>> Create([FromBody] OrganizationCreateDto dto)
    {
        try
        {
            var result = await _organizationService.CreateAsync(dto);
            return ApiResponseDto<OrganizationDto>.Success(result, "创建成功");
        }
        catch (PermissionDeniedException ex)
        {
            return ApiResponseDto<OrganizationDto>.Fail(ex.Message, 403);
        }
        catch (InvalidOperationException ex)
        {
            return ApiResponseDto<OrganizationDto>.Fail(ex.Message, 400);
        }
    }

    /// <summary>
    /// 更新组织
    /// </summary>
    [HttpPut("{id}")]
    [Permission("system:organization:edit")]
    public async Task<ApiResponseDto<OrganizationDto>> Update(long id, [FromBody] OrganizationUpdateDto dto)
    {
        try
        {
            dto.Id = id;
            var result = await _organizationService.UpdateAsync(dto);
            return ApiResponseDto<OrganizationDto>.Success(result, "更新成功");
        }
        catch (PermissionDeniedException ex)
        {
            return ApiResponseDto<OrganizationDto>.Fail(ex.Message, 403);
        }
        catch (InvalidOperationException ex)
        {
            return ApiResponseDto<OrganizationDto>.Fail(ex.Message, 400);
        }
    }

    /// <summary>
    /// 删除组织
    /// </summary>
    [HttpDelete("{id}")]
    [Permission("system:organization:delete")]
    public async Task<ApiResponseDto> Delete(long id)
    {
        try
        {
            await _organizationService.DeleteAsync(id);
            return ApiResponseDto.Success(null, "删除成功");
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
