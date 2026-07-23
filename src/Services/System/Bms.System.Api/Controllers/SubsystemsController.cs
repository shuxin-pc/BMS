using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Bms.System.Application.Dtos;
using Bms.System.Application.Dtos.Subsystems;
using Bms.System.Application.Services;

namespace Bms.System.Api.Controllers;

/// <summary>
/// 子系统管理控制器
/// </summary>
[ApiController]
[Route("api/system/subsystems")]
[Route("api/[controller]")]
[Authorize]
public class SubsystemsController : ControllerBase
{
    private readonly ISubsystemAppService _subsystemAppService;

    public SubsystemsController(ISubsystemAppService subsystemAppService)
    {
        _subsystemAppService = subsystemAppService;
    }

    /// <summary>
    /// 获取子系统列表（分页）
    /// </summary>
    [HttpGet]
    public async Task<ApiResponseDto<List<SubsystemDto>>> GetList([FromQuery] SubsystemQueryDto query)
    {
        // 获取当前用户信息进行租户隔离
        var (isSuperAdmin, tenantId) = GetCurrentUserInfo();
        return await _subsystemAppService.GetListAsync(query, isSuperAdmin, tenantId);
    }

    /// <summary>
    /// 获取所有启用的子系统（下拉选择用）
    /// </summary>
    [HttpGet("all")]
    public async Task<ApiResponseDto<List<SubsystemDto>>> GetAllEnabled()
    {
        // 获取当前用户信息进行租户隔离
        var (isSuperAdmin, tenantId) = GetCurrentUserInfo();
        return await _subsystemAppService.GetAllEnabledAsync(isSuperAdmin, tenantId);
    }

    /// <summary>
    /// 获取所有子系统（不分启用/禁用状态，用于租户子系统分配）
    /// </summary>
    [HttpGet("list-all")]
    public async Task<ApiResponseDto<List<SubsystemDto>>> GetAll()
    {
        // 获取当前用户信息进行租户隔离
        var (isSuperAdmin, tenantId) = GetCurrentUserInfo();
        return await _subsystemAppService.GetAllAsync(isSuperAdmin, tenantId);
    }

    /// <summary>
    /// 获取子系统详情
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ApiResponseDto<SubsystemDto?>> GetById(long id)
    {
        return await _subsystemAppService.GetByIdAsync(id);
    }

    /// <summary>
    /// 获取子系统使用情况（有多少租户使用）
    /// </summary>
    [HttpGet("{id}/usage")]
    public async Task<ApiResponseDto<int>> GetUsageCount(long id)
    {
        return await _subsystemAppService.GetUsageCountAsync(id);
    }

    /// <summary>
    /// 创建子系统（支持内部服务调用）
    /// </summary>
    [HttpPost]
    public async Task<ApiResponseDto<SubsystemDto>> Create([FromBody] SubsystemCreateDto dto)
    {
        // 检查是否为超级管理员
        var (isSuperAdmin, _) = GetCurrentUserInfo();
        if (!isSuperAdmin)
        {
            return ApiResponseDto<SubsystemDto>.Fail("只有超级管理员可以创建子系统", 403);
        }

        try
        {
            return await _subsystemAppService.CreateAsync(dto);
        }
        catch (InvalidOperationException ex)
        {
            return ApiResponseDto<SubsystemDto>.Fail(ex.Message, 400);
        }
    }

    /// <summary>
    /// 更新子系统（仅超级管理员可更新）
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ApiResponseDto<SubsystemDto>> Update(long id, [FromBody] SubsystemUpdateDto dto)
    {
        // 检查是否为超级管理员
        var (isSuperAdmin, _) = GetCurrentUserInfo();
        if (!isSuperAdmin)
        {
            return ApiResponseDto<SubsystemDto>.Fail("只有超级管理员可以更新子系统", 403);
        }

        try
        {
            dto.Id = id;
            return await _subsystemAppService.UpdateAsync(dto);
        }
        catch (InvalidOperationException ex)
        {
            return ApiResponseDto<SubsystemDto>.Fail(ex.Message, 400);
        }
    }

    /// <summary>
    /// 删除子系统（仅超级管理员可删除）
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ApiResponseDto> Delete(long id)
    {
        // 检查是否为超级管理员
        var (isSuperAdmin, _) = GetCurrentUserInfo();
        if (!isSuperAdmin)
        {
            return ApiResponseDto.Fail("只有超级管理员可以删除子系统", 403);
        }

        try
        {
            return await _subsystemAppService.DeleteAsync(id);
        }
        catch (InvalidOperationException ex)
        {
            return ApiResponseDto.Fail(ex.Message, 400);
        }
    }

    /// <summary>
    /// 获取子系统关联的菜单
    /// </summary>
    [HttpGet("{id}/menus")]
    public async Task<ApiResponseDto<List<long>>> GetMenus(long id)
    {
        return await _subsystemAppService.GetMenusAsync(id);
    }

    /// <summary>
    /// 批量设置子系统关联的菜单（覆盖式，支持内部服务调用）
    /// </summary>
    [HttpPut("{id}/menus")]
    public async Task<ApiResponseDto> AssignMenus(long id, [FromBody] SubsystemMenuAssignDto dto)
    {
        // 检查是否为超级管理员
        var (isSuperAdmin, _) = GetCurrentUserInfo();
        if (!isSuperAdmin)
        {
            return ApiResponseDto.Fail("只有超级管理员可以分配子系统菜单", 403);
        }

        try
        {
            return await _subsystemAppService.AssignMenusAsync(id, dto);
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
