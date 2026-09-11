using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Stores;
using Bms.Store.Application.Services;

namespace Bms.Store.Api.Controllers;

/// <summary>
/// 门店设置控制器
/// 管理门店级配置：跨店核销开关（租户级语义）+ 各类型提醒接收角色（子表 StoreReminderSetting，门店级语义）
/// 依赖 X-Store-Id 门店上下文（提醒接收角色按当前门店过滤）
/// </summary>
[ApiController]
[Route("api/store/store-tenant-settings")]
[Authorize]
public class StoreTenantSettingsController : ControllerBase
{
    private readonly IStoreTenantSettingAppService _appService;

    public StoreTenantSettingsController(IStoreTenantSettingAppService appService)
    {
        _appService = appService;
    }

    /// <summary>
    /// 获取当前门店的设置（跨店核销取租户级值）
    /// </summary>
    [HttpGet]
    public async Task<ApiResponseDto<StoreTenantSettingDto>> Get()
        => await _appService.GetAsync();

    /// <summary>
    /// 更新当前门店的设置（不存在时自动创建；跨店核销变更时同步该租户所有门店）
    /// </summary>
    [HttpPut]
    public async Task<ApiResponseDto<StoreTenantSettingDto>> Update([FromBody] StoreTenantSettingUpdateDto dto)
        => await _appService.UpdateAsync(dto);

    /// <summary>
    /// 获取当前门店的各类提醒接收角色配置（子表 StoreReminderSetting）
    /// </summary>
    [HttpGet("reminder-settings")]
    public async Task<ApiResponseDto<List<StoreReminderSettingDto>>> GetReminderSettings()
        => await _appService.GetReminderSettingsAsync();

    /// <summary>
    /// 保存当前门店的各类提醒接收角色（按 ReminderType upsert 到子表）
    /// </summary>
    [HttpPut("reminder-settings")]
    public async Task<ApiResponseDto<List<StoreReminderSettingDto>>> UpdateReminderSettings([FromBody] List<StoreReminderSettingUpdateDto> dtos)
        => await _appService.UpdateReminderSettingsAsync(dtos);
}
