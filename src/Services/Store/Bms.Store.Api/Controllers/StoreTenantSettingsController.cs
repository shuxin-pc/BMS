using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Stores;
using Bms.Store.Application.Services;
using Bms.Store.Api.Filters;

namespace Bms.Store.Api.Controllers;

/// <summary>
/// 租户门店设置控制器
/// 管理租户级跨店权益配置（规则2：AllowCrossStoreVerify 开关）
/// </summary>
[ApiController]
[Route("api/store/store-tenant-settings")]
[Authorize]
[AllowWithoutStore] // 租户级配置不依赖 X-Store-Id
public class StoreTenantSettingsController : ControllerBase
{
    private readonly IStoreTenantSettingAppService _appService;

    public StoreTenantSettingsController(IStoreTenantSettingAppService appService)
    {
        _appService = appService;
    }

    /// <summary>
    /// 获取当前租户的门店设置
    /// </summary>
    [HttpGet]
    public async Task<ApiResponseDto<StoreTenantSettingDto>> Get()
        => await _appService.GetAsync();

    /// <summary>
    /// 更新当前租户的门店设置（不存在时自动创建）
    /// </summary>
    [HttpPut]
    public async Task<ApiResponseDto<StoreTenantSettingDto>> Update([FromBody] StoreTenantSettingUpdateDto dto)
        => await _appService.UpdateAsync(dto);
}
