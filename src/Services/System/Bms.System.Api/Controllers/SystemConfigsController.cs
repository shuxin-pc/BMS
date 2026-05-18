using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Bms.System.Application.Dtos;
using Bms.System.Application.Dtos.SystemConfigs;
using Bms.System.Application.Services;

namespace Bms.System.Api.Controllers;

[ApiController]
[Route("api/system/[controller]")]
[Route("api/[controller]")]
[Authorize]
public class SystemConfigsController : ControllerBase
{
    private readonly ISystemConfigAppService _systemConfigService;
    private readonly ISystemConfigService _configReadService;

    public SystemConfigsController(
        ISystemConfigAppService systemConfigService,
        ISystemConfigService configReadService)
    {
        _systemConfigService = systemConfigService;
        _configReadService = configReadService;
    }

    /// <summary>
    /// 获取当前用户信息
    /// </summary>
    private (long tenantId, string tenantCode, bool isSuperAdmin) GetCurrentUserInfo()
    {
        var tenantIdClaim = User.FindFirst("tenant_id");
        var tenantCodeClaim = User.FindFirst("tenant_code");
        var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        var isSuperAdmin = roles.Contains("super_admin");

        var tenantId = tenantIdClaim != null ? long.Parse(tenantIdClaim.Value) : 1;
        var tenantCode = tenantCodeClaim?.Value ?? "platform";

        return (tenantId, tenantCode, isSuperAdmin);
    }

    /// <summary>
    /// 获取配置详情
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ApiResponseDto<SystemConfigDto?>> GetById(long id)
    {
        return await _systemConfigService.GetByIdAsync(id);
    }

    /// <summary>
    /// 根据配置键获取配置
    /// </summary>
    [HttpGet("key/{configKey}")]
    public async Task<ApiResponseDto<SystemConfigDto?>> GetByKey(string configKey)
    {
        return await _systemConfigService.GetByKeyAsync(configKey);
    }

    /// <summary>
    /// 获取配置分页列表
    /// </summary>
    [HttpGet]
    public async Task<ApiResponseDto<PagedResponseDto<SystemConfigDto>>> GetPagedList([FromQuery] PagedRequestDto pagedRequest, [FromQuery] SystemConfigQueryDto? query)
    {
        var (tenantId, _, isSuperAdmin) = GetCurrentUserInfo();
        return await _systemConfigService.GetPagedListAsync(pagedRequest, query, tenantId, isSuperAdmin);
    }

    /// <summary>
    /// 根据分组获取配置列表
    /// </summary>
    [HttpGet("group/{configGroup}")]
    public async Task<ApiResponseDto<List<SystemConfigDto>>> GetByGroup(string configGroup)
    {
        return await _systemConfigService.GetByGroupAsync(configGroup);
    }

    /// <summary>
    /// 获取公共配置列表
    /// </summary>
    [HttpGet("public")]
    [AllowAnonymous]
    public async Task<ApiResponseDto<List<SystemConfigDto>>> GetPublicConfigs()
    {
        return await _systemConfigService.GetPublicConfigsAsync();
    }

    /// <summary>
    /// 获取系统配置（用于配置应用，按租户优先级返回）
    /// 未登录用户返回平台租户默认配置
    /// </summary>
    [HttpGet("system")]
    [AllowAnonymous]
    public async Task<ApiResponseDto<Dictionary<string, string>>> GetSystemConfigs()
    {
        var (tenantId, _, _) = GetCurrentUserInfo();
        var configs = await _configReadService.GetSystemConfigsAsync(tenantId);
        return ApiResponseDto<Dictionary<string, string>>.Success(configs);
    }

    /// <summary>
    /// 创建配置
    /// </summary>
    [HttpPost]
    public async Task<ApiResponseDto<SystemConfigDto>> Create([FromBody] SystemConfigCreateDto dto)
    {
        try
        {
            var (tenantId, tenantCode, isSuperAdmin) = GetCurrentUserInfo();
            return await _systemConfigService.CreateAsync(dto, tenantId, tenantCode, isSuperAdmin);
        }
        catch (InvalidOperationException ex)
        {
            return ApiResponseDto<SystemConfigDto>.Fail(ex.Message, 400);
        }
    }

    /// <summary>
    /// 更新配置
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ApiResponseDto<SystemConfigDto>> Update(long id, [FromBody] SystemConfigUpdateDto dto)
    {
        try
        {
            var (tenantId, tenantCode, isSuperAdmin) = GetCurrentUserInfo();
            dto.Id = id;
            return await _systemConfigService.UpdateAsync(dto, tenantId, tenantCode, isSuperAdmin);
        }
        catch (InvalidOperationException ex)
        {
            return ApiResponseDto<SystemConfigDto>.Fail(ex.Message, 400);
        }
    }

    /// <summary>
    /// 删除配置
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ApiResponseDto> Delete(long id)
    {
        var (tenantId, _, isSuperAdmin) = GetCurrentUserInfo();
        return await _systemConfigService.DeleteAsync(id, tenantId, isSuperAdmin);
    }
}
