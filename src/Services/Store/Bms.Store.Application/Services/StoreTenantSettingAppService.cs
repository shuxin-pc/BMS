using Mapster;
using Microsoft.EntityFrameworkCore;
using Bms.BuildingBlocks.Abstractions.Security;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Stores;
using Bms.Store.Domain.Entities;
using Bms.Store.Infrastructure;

namespace Bms.Store.Application.Services;

/// <summary>
/// 租户门店设置应用服务实现
/// 管理租户级跨店权益配置（规则2：AllowCrossStoreVerify 开关）
/// 每租户至多一条记录，不存在时按默认值（允许跨店核销）返回，首次更新时自动创建
/// </summary>
public class StoreTenantSettingAppService : IStoreTenantSettingAppService
{
    private readonly StoreDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public StoreTenantSettingAppService(StoreDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    /// <summary>
    /// 获取当前租户的门店设置
    /// 不存在时返回默认值（AllowCrossStoreVerify=true），不自动落库
    /// </summary>
    public async Task<ApiResponseDto<StoreTenantSettingDto>> GetAsync()
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<StoreTenantSettingDto>.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;
        var setting = await _dbContext.StoreTenantSettings
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && !s.IsDeleted);

        var dto = setting != null
            ? setting.Adapt<StoreTenantSettingDto>()
            : new StoreTenantSettingDto { AllowCrossStoreVerify = true, BirthdayReminderRoleIds = new List<long>() };

        // 旧记录可能 BirthdayReminderRoleIds 为 null（迁移新增列默认 nullable），兜底为空列表
        dto.BirthdayReminderRoleIds ??= new List<long>();

        return ApiResponseDto<StoreTenantSettingDto>.Ok(dto);
    }

    /// <summary>
    /// 更新当前租户的门店设置
    /// 不存在时自动创建（首次配置）；存在时更新开关值
    /// </summary>
    public async Task<ApiResponseDto<StoreTenantSettingDto>> UpdateAsync(StoreTenantSettingUpdateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<StoreTenantSettingDto>.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;
        var setting = await _dbContext.StoreTenantSettings
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && !s.IsDeleted);

        if (setting == null)
        {
            setting = new StoreTenantSetting
            {
                TenantId = tenantId,
                AllowCrossStoreVerify = dto.AllowCrossStoreVerify,
                BirthdayReminderRoleIds = dto.BirthdayReminderRoleIds ?? new List<long>()
            };
            await _dbContext.StoreTenantSettings.AddAsync(setting);
        }
        else
        {
            setting.AllowCrossStoreVerify = dto.AllowCrossStoreVerify;
            setting.BirthdayReminderRoleIds = dto.BirthdayReminderRoleIds ?? new List<long>();
            setting.UpdatedTime = DateTime.Now;
        }

        await _dbContext.SaveChangesAsync();
        return ApiResponseDto<StoreTenantSettingDto>.Ok(setting.Adapt<StoreTenantSettingDto>());
    }
}
