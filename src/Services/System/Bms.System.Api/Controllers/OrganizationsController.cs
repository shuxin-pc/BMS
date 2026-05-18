using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Bms.System.Application.Dtos;
using Bms.System.Application.Dtos.Organizations;
using Bms.System.Application.Services;

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
    public async Task<ApiResponseDto<List<OrganizationDto>>> GetTree([FromQuery] OrganizationQueryDto? query)
    {
        // 获取当前用户信息进行租户隔离
        var (isSuperAdmin, tenantId) = GetCurrentUserInfo();

        // 租户隔离：非超级管理员只能看到当前租户的组织
        // 超级管理员如果传了 tenantId，按租户筛选；否则返回所有可见组织
        long? effectiveTenantId = null;
        if (!isSuperAdmin && tenantId.HasValue)
        {
            // 非超级管理员：如果请求中没有指定租户筛选条件，默认使用当前租户
            effectiveTenantId = query?.TenantId ?? tenantId;
        }
        else if (query?.TenantId.HasValue == true)
        {
            // 超级管理员或非超级管理员传了 tenantId：按租户筛选
            effectiveTenantId = query.TenantId;
        }

        var result = await _organizationService.GetTreeListAsync(query, isSuperAdmin, effectiveTenantId);
        return ApiResponseDto<List<OrganizationDto>>.Success(result);
    }

    /// <summary>
    /// 获取组织列表
    /// </summary>
    [HttpGet]
    public async Task<ApiResponseDto<List<OrganizationDto>>> GetList([FromQuery] OrganizationQueryDto query)
    {
        // 获取当前用户信息进行租户隔离
        var (isSuperAdmin, tenantId) = GetCurrentUserInfo();

        // 租户隔离：非超级管理员只能看到当前租户的组织
        // 超级管理员如果传了 tenantId，按租户筛选；否则不限制
        long? effectiveTenantId = null;
        if (!isSuperAdmin && tenantId.HasValue)
        {
            // 非超级管理员：如果请求中没有指定租户筛选条件，默认使用当前租户
            effectiveTenantId = query.TenantId ?? tenantId;
        }
        else if (isSuperAdmin && query.TenantId.HasValue)
        {
            // 超级管理员：只有明确传了 tenantId 才筛选
            effectiveTenantId = query.TenantId;
        }

        var result = await _organizationService.GetListAsync(query, isSuperAdmin, effectiveTenantId);
        return ApiResponseDto<List<OrganizationDto>>.Success(result);
    }

    /// <summary>
    /// 获取组织详情
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ApiResponseDto<OrganizationDto>> GetById(long id)
    {
        var result = await _organizationService.GetByIdAsync(id);
        return ApiResponseDto<OrganizationDto>.Success(result);
    }

    /// <summary>
    /// 获取子组织列表
    /// </summary>
    [HttpGet("{parentId}/children")]
    public async Task<ApiResponseDto<List<OrganizationDto>>> GetChildren(long parentId)
    {
        var result = await _organizationService.GetChildrenAsync(parentId);
        return ApiResponseDto<List<OrganizationDto>>.Success(result);
    }

    /// <summary>
    /// 创建组织
    /// </summary>
    [HttpPost]
    public async Task<ApiResponseDto<OrganizationDto>> Create([FromBody] OrganizationCreateDto dto)
    {
        try
        {
            var result = await _organizationService.CreateAsync(dto);
            return ApiResponseDto<OrganizationDto>.Success(result, "创建成功");
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
    public async Task<ApiResponseDto<OrganizationDto>> Update(long id, [FromBody] OrganizationUpdateDto dto)
    {
        try
        {
            dto.Id = id;
            var result = await _organizationService.UpdateAsync(dto);
            return ApiResponseDto<OrganizationDto>.Success(result, "更新成功");
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
    public async Task<ApiResponseDto> Delete(long id)
    {
        try
        {
            await _organizationService.DeleteAsync(id);
            return ApiResponseDto.Success(null, "删除成功");
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
