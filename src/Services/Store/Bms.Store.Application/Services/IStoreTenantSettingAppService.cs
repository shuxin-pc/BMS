using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Stores;

namespace Bms.Store.Application.Services;

/// <summary>
/// 门店设置应用服务接口
/// 管理门店级配置：跨店核销开关（租户级语义）+ 各类型提醒接收角色（子表 StoreReminderSetting，按 StoreId 过滤）
/// </summary>
public interface IStoreTenantSettingAppService
{
    /// <summary>
    /// 获取当前门店的设置（不存在时返回默认值）
    /// </summary>
    Task<ApiResponseDto<StoreTenantSettingDto>> GetAsync();

    /// <summary>
    /// 更新当前门店的设置（不存在时自动创建）
    /// </summary>
    Task<ApiResponseDto<StoreTenantSettingDto>> UpdateAsync(StoreTenantSettingUpdateDto dto);

    /// <summary>
    /// 获取当前门店的各类提醒接收角色配置（子表 StoreReminderSetting，未配置类型不返回）
    /// </summary>
    Task<ApiResponseDto<List<StoreReminderSettingDto>>> GetReminderSettingsAsync();

    /// <summary>
    /// 保存当前门店的各类提醒接收角色（按 ReminderType upsert 到子表）
    /// </summary>
    Task<ApiResponseDto<List<StoreReminderSettingDto>>> UpdateReminderSettingsAsync(List<StoreReminderSettingUpdateDto> dtos);
}
