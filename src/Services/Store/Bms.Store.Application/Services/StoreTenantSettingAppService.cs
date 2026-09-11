using Mapster;
using Microsoft.EntityFrameworkCore;
using Bms.BuildingBlocks.Abstractions.Security;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Stores;
using Bms.Store.Domain.Entities;
using Bms.Store.Infrastructure;

namespace Bms.Store.Application.Services;

/// <summary>
/// 门店设置应用服务实现
/// 管理门店级配置：
/// - 跨店核销开关：租户级语义（读取/判断时忽略门店，取租户第一条记录；保存时同步该租户所有门店记录）
/// - 各类型提醒接收角色：拆分至子表 StoreReminderSetting（每门店+每业务类型一行），独立于主表懒创建
/// 主表记录不存在时按默认值（允许跨店核销）返回，首次更新时自动创建
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
    /// 获取当前门店的设置
    /// 跨店核销取该租户任意一条记录（忽略门店，无记录默认 true）
    /// </summary>
    public async Task<ApiResponseDto<StoreTenantSettingDto>> GetAsync()
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<StoreTenantSettingDto>.Fail("登录状态异常，请重新登录", 401);
        if (!_currentUser.StoreId.HasValue)
            return ApiResponseDto<StoreTenantSettingDto>.Fail("未绑定门店，请联系管理员绑定门店后再访问此功能", 400);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId.Value;

        // 跨店核销为租户级语义：取该租户第一条记录（忽略门店），无记录时默认允许
        var tenantSetting = await _dbContext.StoreTenantSettings
            .FirstOrDefaultAsync(s => s.TenantId == tenantId);
        var allowCrossStoreVerify = tenantSetting?.AllowCrossStoreVerify ?? true;

        // 主表记录（当前门店，用于 Id/时间戳展示，无记录时返回默认值）
        var storeSetting = await _dbContext.StoreTenantSettings
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.StoreId == storeId);

        var dto = new StoreTenantSettingDto
        {
            Id = storeSetting?.Id ?? 0,
            StoreId = storeId,
            AllowCrossStoreVerify = allowCrossStoreVerify,
            CreatedAt = storeSetting?.CreatedTime ?? default,
            UpdatedAt = storeSetting?.UpdatedTime
        };

        return ApiResponseDto<StoreTenantSettingDto>.Ok(dto);
    }

    /// <summary>
    /// 更新当前门店的设置（不存在时自动创建）
    /// 跨店核销变更时同步该租户所有门店记录，保证租户级开关一致
    /// </summary>
    public async Task<ApiResponseDto<StoreTenantSettingDto>> UpdateAsync(StoreTenantSettingUpdateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<StoreTenantSettingDto>.Fail("登录状态异常，请重新登录", 401);
        if (!_currentUser.StoreId.HasValue)
            return ApiResponseDto<StoreTenantSettingDto>.Fail("未绑定门店，请联系管理员绑定门店后再访问此功能", 400);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId.Value;

        var setting = await _dbContext.StoreTenantSettings
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.StoreId == storeId);

        if (setting == null)
        {
            // 新建当前门店记录：门店编码从 Stores 表读取，保证 StoreId/StoreCode 一致
            var storeCode = await GetStoreCodeAsync(tenantId, storeId);

            setting = new StoreTenantSetting
            {
                TenantId = tenantId,
                TenantCode = _currentUser.TenantCode ?? string.Empty,
                StoreId = storeId,
                StoreCode = storeCode,
                AllowCrossStoreVerify = dto.AllowCrossStoreVerify
            };
            await _dbContext.StoreTenantSettings.AddAsync(setting);
        }
        else
        {
            setting.AllowCrossStoreVerify = dto.AllowCrossStoreVerify;
            setting.UpdatedTime = DateTime.Now;
        }

        // 跨店核销为租户级语义：同步该租户所有其他门店记录的开关值，避免租户内开关分裂
        var otherSettings = await _dbContext.StoreTenantSettings
            .Where(s => s.TenantId == tenantId && s.Id != setting.Id)
            .ToListAsync();
        foreach (var other in otherSettings)
        {
            other.AllowCrossStoreVerify = dto.AllowCrossStoreVerify;
            other.UpdatedTime = DateTime.Now;
        }

        await _dbContext.SaveChangesAsync();
        return ApiResponseDto<StoreTenantSettingDto>.Ok(setting.Adapt<StoreTenantSettingDto>());
    }

    /// <summary>
    /// 获取当前门店的各类提醒接收角色配置（子表 StoreReminderSetting）
    /// 返回当前门店所有未删除的提醒配置，供前端按类型循环渲染；未配置的类型由前端兜底为空列表
    /// </summary>
    public async Task<ApiResponseDto<List<StoreReminderSettingDto>>> GetReminderSettingsAsync()
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<List<StoreReminderSettingDto>>.Fail("登录状态异常，请重新登录", 401);
        if (!_currentUser.StoreId.HasValue)
            return ApiResponseDto<List<StoreReminderSettingDto>>.Fail("未绑定门店，请联系管理员绑定门店后再访问此功能", 400);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId.Value;

        var settings = await _dbContext.StoreReminderSettings
            .Where(s => s.TenantId == tenantId && s.StoreId == storeId && !s.IsDeleted)
            .Select(s => new StoreReminderSettingDto
            {
                ReminderType = s.ReminderType,
                RoleIds = s.RoleIds
            })
            .ToListAsync();

        return ApiResponseDto<List<StoreReminderSettingDto>>.Ok(settings);
    }

    /// <summary>
    /// 保存当前门店的各类提醒接收角色（子表 StoreReminderSetting，upsert）
    /// 按 ReminderType 逐条 upsert 到子表：存在则更新 RoleIds，不存在则创建（独立于主表懒创建，无主表行也可保存）
    /// </summary>
    public async Task<ApiResponseDto<List<StoreReminderSettingDto>>> UpdateReminderSettingsAsync(List<StoreReminderSettingUpdateDto> dtos)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<List<StoreReminderSettingDto>>.Fail("登录状态异常，请重新登录", 401);
        if (!_currentUser.StoreId.HasValue)
            return ApiResponseDto<List<StoreReminderSettingDto>>.Fail("未绑定门店，请联系管理员绑定门店后再访问此功能", 400);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId.Value;

        if (dtos == null || dtos.Count == 0)
            return ApiResponseDto<List<StoreReminderSettingDto>>.Ok(new List<StoreReminderSettingDto>());

        var storeCode = await GetStoreCodeAsync(tenantId, storeId);

        foreach (var dto in dtos)
        {
            if (string.IsNullOrWhiteSpace(dto.ReminderType))
                continue;

            var setting = await _dbContext.StoreReminderSettings
                .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.StoreId == storeId
                    && s.ReminderType == dto.ReminderType && !s.IsDeleted);

            var roleIds = dto.RoleIds ?? new List<long>();
            if (setting == null)
            {
                setting = new StoreReminderSetting
                {
                    TenantId = tenantId,
                    TenantCode = _currentUser.TenantCode ?? string.Empty,
                    StoreId = storeId,
                    StoreCode = storeCode,
                    ReminderType = dto.ReminderType,
                    RoleIds = roleIds
                };
                await _dbContext.StoreReminderSettings.AddAsync(setting);
            }
            else
            {
                setting.RoleIds = roleIds;
                setting.UpdatedTime = DateTime.Now;
            }
        }

        await _dbContext.SaveChangesAsync();
        return await GetReminderSettingsAsync();
    }

    /// <summary>
    /// 读取门店编码（用于新建主表/子表记录时保证 StoreId/StoreCode 一致）
    /// </summary>
    private async Task<string> GetStoreCodeAsync(long tenantId, long storeId)
    {
        return await _dbContext.Stores
            .Where(s => s.Id == storeId && !s.IsDeleted)
            .Select(s => s.Code)
            .FirstOrDefaultAsync() ?? string.Empty;
    }
}
