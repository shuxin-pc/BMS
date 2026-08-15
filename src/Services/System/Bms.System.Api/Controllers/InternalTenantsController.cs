using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bms.System.Application.Dtos;
using Bms.System.Application.Dtos.Tenants;
using Bms.System.Application.Services;

namespace Bms.System.Api.Controllers;

/// <summary>
/// 内部租户查询控制器（供网关调用，通过 X-Internal-Service headers 认证）
/// </summary>
[ApiController]
[Route("api/internal/tenants")]
[ApiExplorerSettings(IgnoreApi = true)]
[Authorize] // 内部接口需认证（由 InternalServiceAuthMiddleware 设置身份）
public class InternalTenantsController : ControllerBase
{
    private readonly ITenantAppService _tenantService;

    public InternalTenantsController(ITenantAppService tenantService)
    {
        _tenantService = tenantService;
    }

    /// <summary>
    /// 根据租户ID获取租户信息（内部接口）
    /// </summary>
    [HttpGet("{id:long}")]
    public async Task<ApiResponseDto<TenantDto?>> GetById(long id)
    {
        return await _tenantService.GetByIdAsync(id);
    }

    /// <summary>
    /// 根据租户编码获取租户信息（内部接口）
    /// </summary>
    [HttpGet("code/{code}")]
    public async Task<ApiResponseDto<TenantDto?>> GetByCode(string code)
    {
        return await _tenantService.GetByCodeAsync(code);
    }
}
