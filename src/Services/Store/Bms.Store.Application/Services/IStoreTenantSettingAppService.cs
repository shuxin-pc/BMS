using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Stores;

namespace Bms.Store.Application.Services;

/// <summary>
/// 租户门店设置应用服务接口
/// 管理租户级跨店权益配置（规则2：AllowCrossStoreVerify 开关）
/// </summary>
public interface IStoreTenantSettingAppService
{
    /// <summary>
    /// 获取当前租户的门店设置（不存在时返回默认值）
    /// </summary>
    Task<ApiResponseDto<StoreTenantSettingDto>> GetAsync();

    /// <summary>
    /// 更新当前租户的门店设置（不存在时自动创建）
    /// </summary>
    Task<ApiResponseDto<StoreTenantSettingDto>> UpdateAsync(StoreTenantSettingUpdateDto dto);
}
