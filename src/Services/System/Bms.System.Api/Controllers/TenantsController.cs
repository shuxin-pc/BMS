using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Bms.System.Application.Dtos;
using Bms.System.Application.Dtos.Tenants;
using Bms.System.Application.Services;

namespace Bms.System.Api.Controllers;

/// <summary>
/// 租户管理控制器（仅超级管理员可访问）
/// </summary>
[ApiController]
[Route("api/system/[controller]")]
[Route("api/[controller]")]
[Authorize]
public class TenantsController : ControllerBase
{
    private readonly ITenantAppService _tenantService;
    private readonly ITenantSubsystemAppService _tenantSubsystemAppService;

    public TenantsController(
        ITenantAppService tenantService,
        ITenantSubsystemAppService tenantSubsystemAppService)
    {
        _tenantService = tenantService;
        _tenantSubsystemAppService = tenantSubsystemAppService;
    }

    /// <summary>
    /// 获取租户分页列表（仅超级管理员可访问）
    /// </summary>
    [HttpGet]
    public async Task<ApiResponseDto<PagedResponseDto<TenantDto>>> GetList([FromQuery] PagedRequestDto request)
    {
        // 检查是否为超级管理员
        if (!IsSuperAdmin())
        {
            return ApiResponseDto<PagedResponseDto<TenantDto>>.Fail("只有超级管理员可以访问租户管理", 403);
        }
        return await _tenantService.GetPagedListAsync(request);
    }

    /// <summary>
    /// 获取租户详情（仅超级管理员可访问）
    /// </summary>
    [HttpGet("{id:long}")]
    public async Task<ApiResponseDto<TenantDto?>> GetById(long id)
    {
        if (!IsSuperAdmin())
        {
            return ApiResponseDto<TenantDto?>.Fail("只有超级管理员可以访问租户管理", 403);
        }
        return await _tenantService.GetByIdAsync(id);
    }

    /// <summary>
    /// 根据编码获取租户（仅超级管理员可访问）
    /// </summary>
    [HttpGet("code/{code}")]
    public async Task<ApiResponseDto<TenantDto?>> GetByCode(string code)
    {
        if (!IsSuperAdmin())
        {
            return ApiResponseDto<TenantDto?>.Fail("只有超级管理员可以访问租户管理", 403);
        }
        return await _tenantService.GetByCodeAsync(code);
    }

    /// <summary>
    /// 创建租户（仅超级管理员可访问）
    /// </summary>
    [HttpPost]
    public async Task<ApiResponseDto<TenantDto>> Create([FromBody] TenantCreateDto dto)
    {
        if (!IsSuperAdmin())
        {
            return ApiResponseDto<TenantDto>.Fail("只有超级管理员可以访问租户管理", 403);
        }
        try
        {
            return await _tenantService.CreateAsync(dto);
        }
        catch (InvalidOperationException ex)
        {
            return ApiResponseDto<TenantDto>.Fail(ex.Message, 400);
        }
    }

    /// <summary>
    /// 更新租户（仅超级管理员可访问）
    /// </summary>
    [HttpPut("{id:long}")]
    public async Task<ApiResponseDto<TenantDto>> Update(long id, [FromBody] TenantUpdateDto dto)
    {
        if (!IsSuperAdmin())
        {
            return ApiResponseDto<TenantDto>.Fail("只有超级管理员可以访问租户管理", 403);
        }
        try
        {
            dto.Id = id;
            return await _tenantService.UpdateAsync(dto);
        }
        catch (InvalidOperationException ex)
        {
            return ApiResponseDto<TenantDto>.Fail(ex.Message, 400);
        }
    }

    /// <summary>
    /// 删除租户（仅超级管理员可访问）
    /// </summary>
    [HttpDelete("{id:long}")]
    public async Task<ApiResponseDto> Delete(long id)
    {
        if (!IsSuperAdmin())
        {
            return ApiResponseDto.Fail("只有超级管理员可以访问租户管理", 403);
        }
        try
        {
            return await _tenantService.DeleteAsync(id);
        }
        catch (InvalidOperationException ex)
        {
            return ApiResponseDto.Fail(ex.Message, 400);
        }
    }

    /// <summary>
    /// 批量删除租户（仅超级管理员可访问）
    /// </summary>
    [HttpPost("batch")]
    public async Task<ApiResponseDto> BatchDelete([FromBody] BatchDeleteRequest request)
    {
        if (!IsSuperAdmin())
        {
            return ApiResponseDto.Fail("只有超级管理员可以访问租户管理", 403);
        }
        try
        {
            return await _tenantService.BatchDeleteAsync(request.Ids);
        }
        catch (InvalidOperationException ex)
        {
            return ApiResponseDto.Fail(ex.Message, 400);
        }
    }

    /// <summary>
    /// 启用租户（仅超级管理员可访问）
    /// </summary>
    [HttpPost("{id:long}/enable")]
    public async Task<ApiResponseDto> Enable(long id)
    {
        if (!IsSuperAdmin())
        {
            return ApiResponseDto.Fail("只有超级管理员可以访问租户管理", 403);
        }
        try
        {
            return await _tenantService.EnableAsync(id);
        }
        catch (InvalidOperationException ex)
        {
            return ApiResponseDto.Fail(ex.Message, 400);
        }
    }

    /// <summary>
    /// 禁用租户（仅超级管理员可访问）
    /// </summary>
    [HttpPost("{id:long}/disable")]
    public async Task<ApiResponseDto> Disable(long id)
    {
        if (!IsSuperAdmin())
        {
            return ApiResponseDto.Fail("只有超级管理员可以访问租户管理", 403);
        }
        try
        {
            return await _tenantService.DisableAsync(id);
        }
        catch (InvalidOperationException ex)
        {
            return ApiResponseDto.Fail(ex.Message, 400);
        }
    }

    /// <summary>
    /// 获取租户的子系统列表（仅超级管理员可访问）
    /// </summary>
    [HttpGet("{id:long}/subsystems")]
    public async Task<ApiResponseDto<List<long>>> GetSubsystems(long id)
    {
        if (!IsSuperAdmin())
        {
            return ApiResponseDto<List<long>>.Fail("只有超级管理员可以访问租户管理", 403);
        }
        return await _tenantSubsystemAppService.GetByTenantIdAsync(id);
    }

    /// <summary>
    /// 批量更新租户的子系统（替换式，仅超级管理员可访问）
    /// </summary>
    [HttpPut("{id:long}/subsystems")]
    public async Task<ApiResponseDto> UpdateSubsystems(long id, [FromBody] TenantSubsystemAssignDto dto)
    {
        if (!IsSuperAdmin())
        {
            return ApiResponseDto.Fail("只有超级管理员可以访问租户管理", 403);
        }
        try
        {
            return await _tenantSubsystemAppService.AssignSubsystemsAsync(id, dto);
        }
        catch (InvalidOperationException ex)
        {
            return ApiResponseDto.Fail(ex.Message, 400);
        }
    }

    /// <summary>
    /// 为租户分配单个子系统（仅超级管理员可访问）
    /// </summary>
    [HttpPost("{id:long}/subsystems/{subsystemId:long}")]
    public async Task<ApiResponseDto> AddSubsystem(long id, long subsystemId)
    {
        if (!IsSuperAdmin())
        {
            return ApiResponseDto.Fail("只有超级管理员可以访问租户管理", 403);
        }
        try
        {
            return await _tenantSubsystemAppService.AddSubsystemAsync(id, subsystemId);
        }
        catch (InvalidOperationException ex)
        {
            return ApiResponseDto.Fail(ex.Message, 400);
        }
    }

    /// <summary>
    /// 批量为租户分配子系统（仅超级管理员可访问）
    /// </summary>
    [HttpPost("{id:long}/subsystems/batch")]
    public async Task<ApiResponseDto> BatchAddSubsystems(long id, [FromBody] TenantSubsystemAssignDto dto)
    {
        if (!IsSuperAdmin())
        {
            return ApiResponseDto.Fail("只有超级管理员可以访问租户管理", 403);
        }
        try
        {
            return await _tenantSubsystemAppService.BatchAddSubsystemsAsync(id, dto.SubsystemIds);
        }
        catch (InvalidOperationException ex)
        {
            return ApiResponseDto.Fail(ex.Message, 400);
        }
    }

    /// <summary>
    /// 取消租户的单个子系统（仅超级管理员可访问）
    /// </summary>
    [HttpDelete("{id:long}/subsystems/{subsystemId:long}")]
    public async Task<ApiResponseDto> RemoveSubsystem(long id, long subsystemId)
    {
        if (!IsSuperAdmin())
        {
            return ApiResponseDto.Fail("只有超级管理员可以访问租户管理", 403);
        }
        try
        {
            return await _tenantSubsystemAppService.RemoveSubsystemAsync(id, subsystemId);
        }
        catch (InvalidOperationException ex)
        {
            return ApiResponseDto.Fail(ex.Message, 400);
        }
    }

    /// <summary>
    /// 批量取消租户的子系统（仅超级管理员可访问）
    /// </summary>
    [HttpDelete("{id:long}/subsystems")]
    public async Task<ApiResponseDto> BatchRemoveSubsystems(long id, [FromBody] TenantSubsystemAssignDto dto)
    {
        if (!IsSuperAdmin())
        {
            return ApiResponseDto.Fail("只有超级管理员可以访问租户管理", 403);
        }
        try
        {
            return await _tenantSubsystemAppService.BatchRemoveSubsystemsAsync(id, dto.SubsystemIds);
        }
        catch (InvalidOperationException ex)
        {
            return ApiResponseDto.Fail(ex.Message, 400);
        }
    }

    /// <summary>
    /// 检查是否为超级管理员
    /// </summary>
    private bool IsSuperAdmin()
    {
        var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        return roles.Contains("super_admin");
    }
}
