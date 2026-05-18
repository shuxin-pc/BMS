using Mapster;
using Bms.System.Application.Dtos;
using Bms.System.Application.Dtos.SystemConfigs;
using Bms.System.Domain.Entities;
using Bms.System.Domain.IRepositories;

namespace Bms.System.Application.Services;

public class SystemConfigAppService : ISystemConfigAppService
{
    private readonly ISystemConfigRepository _systemConfigRepository;
    /// <summary>
    /// 平台租户ID（固定值）
    /// </summary>
    private const long PlatformTenantId = 1;

    public SystemConfigAppService(ISystemConfigRepository systemConfigRepository)
    {
        _systemConfigRepository = systemConfigRepository;
    }

    public async Task<ApiResponseDto<SystemConfigDto?>> GetByIdAsync(long id)
    {
        var config = await _systemConfigRepository.GetByIdAsync(id);
        if (config == null)
        {
            return ApiResponseDto<SystemConfigDto?>.Fail("配置不存在", 404);
        }
        return ApiResponseDto<SystemConfigDto?>.Success(config.Adapt<SystemConfigDto>());
    }

    public async Task<ApiResponseDto<SystemConfigDto?>> GetByKeyAsync(string configKey)
    {
        var config = await _systemConfigRepository.GetByKeyAsync(configKey);
        if (config == null)
        {
            return ApiResponseDto<SystemConfigDto?>.Fail("配置不存在", 404);
        }
        return ApiResponseDto<SystemConfigDto?>.Success(config.Adapt<SystemConfigDto>());
    }

    public async Task<ApiResponseDto<List<SystemConfigDto>>> GetListAsync()
    {
        var configs = await _systemConfigRepository.GetListAsync();
        return ApiResponseDto<List<SystemConfigDto>>.Success(configs.Select(c => c.Adapt<SystemConfigDto>()).ToList());
    }

    public async Task<ApiResponseDto<PagedResponseDto<SystemConfigDto>>> GetPagedListAsync(PagedRequestDto request, SystemConfigQueryDto? query, long currentTenantId, bool isSuperAdmin)
    {
        var allConfigs = await _systemConfigRepository.GetListAsync();

        // 超级管理员：不做可见性过滤，只根据界面条件筛选
        // 非超级管理员：根据 IsPublic 和 TenantId 进行可见性过滤
        IEnumerable<SystemConfig> filteredConfigs;

        if (isSuperAdmin)
        {
            // 超级管理员：不做可见性过滤
            filteredConfigs = allConfigs;

            // 如果超级管理员指定了 tenantId，则按租户筛选
            if (query?.TenantId.HasValue == true)
            {
                filteredConfigs = filteredConfigs.Where(c => c.TenantId == query.TenantId.Value);
            }
        }
        else
        {
            // 非超级管理员：根据可见性规则过滤
            // 规则：IsPublic=true 所有用户可见；IsPublic=false 仅同 TenantId 用户可见
            // 优先级：相同 ConfigKey 存在时，仅显示该租户的私有配置
            var visibleConfigs = allConfigs
                .Where(c =>
                    c.IsPublic ||  // 公开配置所有用户可见
                    c.TenantId == currentTenantId)  // 或属于当前租户的私有配置
                .ToList();

            // 处理 ConfigKey 优先级：同租户的私有配置优先于公开配置
            filteredConfigs = visibleConfigs
                .GroupBy(c => c.ConfigKey.ToLower())
                .Select(g =>
                {
                    // 如果存在同租户的私有配置，只返回该租户的私有配置
                    var tenantPrivateConfig = g.FirstOrDefault(c => c.TenantId == currentTenantId && !c.IsPublic);
                    if (tenantPrivateConfig != null)
                    {
                        return tenantPrivateConfig;
                    }
                    // 否则返回公开配置
                    return g.FirstOrDefault(c => c.IsPublic) ?? g.First();
                })
                .ToList();
        }

        // 应用查询条件过滤
        if (query != null)
        {
            if (!string.IsNullOrEmpty(query.ConfigKey))
            {
                filteredConfigs = filteredConfigs
                    .Where(c => c.ConfigKey.ToLower().Contains(query.ConfigKey.ToLower()))
                    .ToList();
            }

            if (!string.IsNullOrEmpty(query.ConfigGroup))
            {
                filteredConfigs = filteredConfigs
                    .Where(c => c.ConfigGroup == query.ConfigGroup)
                    .ToList();
            }
        }

        var totalCount = filteredConfigs.Count();

        // 分页
        var pagedConfigs = filteredConfigs
            .Skip((request.PageIndex - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        var result = new PagedResponseDto<SystemConfigDto>
        {
            List = pagedConfigs.Select(c => c.Adapt<SystemConfigDto>()).ToList(),
            Total = totalCount,
            PageIndex = request.PageIndex,
            PageSize = request.PageSize
        };

        return ApiResponseDto<PagedResponseDto<SystemConfigDto>>.Success(result);
    }

    public async Task<ApiResponseDto<List<SystemConfigDto>>> GetByGroupAsync(string configGroup)
    {
        var configs = await _systemConfigRepository.GetByGroupAsync(configGroup);
        return ApiResponseDto<List<SystemConfigDto>>.Success(configs.Select(c => c.Adapt<SystemConfigDto>()).ToList());
    }

    public async Task<ApiResponseDto<List<SystemConfigDto>>> GetPublicConfigsAsync()
    {
        var configs = await _systemConfigRepository.GetPublicConfigsAsync();
        return ApiResponseDto<List<SystemConfigDto>>.Success(configs.Select(c => c.Adapt<SystemConfigDto>()).ToList());
    }

    public async Task<ApiResponseDto<SystemConfigDto>> CreateAsync(SystemConfigCreateDto dto, long currentTenantId, string currentTenantCode, bool isSuperAdmin)
    {
        // 仅超级管理员可新增配置
        if (!isSuperAdmin)
        {
            throw new InvalidOperationException("只有超级管理员才能新增配置");
        }

        if (await _systemConfigRepository.ExistsKeyAsync(dto.ConfigKey))
        {
            throw new InvalidOperationException($"配置键 {dto.ConfigKey} 已存在");
        }

        // 当新增 DefaultPageSize 时，验证值必须在 DefaultPageSizes 列表中
        if (dto.ConfigKey.ToLower() == "defaultpagesize")
        {
            var pageSizesConfig = await _systemConfigRepository.GetByKeyAsync("DefaultPageSizes");
            if (pageSizesConfig != null && !string.IsNullOrEmpty(pageSizesConfig.ConfigValue))
            {
                var allowedSizes = pageSizesConfig.ConfigValue.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .ToList();

                if (!allowedSizes.Contains(dto.ConfigValue.Trim()))
                {
                    throw new InvalidOperationException($"DefaultPageSize 的值必须在 DefaultPageSizes 列表中（当前允许值：{pageSizesConfig.ConfigValue}）");
                }
            }
        }

        var config = new SystemConfig
        {
            ConfigKey = dto.ConfigKey,
            ConfigValue = dto.ConfigValue,
            ConfigGroup = dto.ConfigGroup,
            Description = dto.Description,
            IsPublic = dto.IsPublic,
            Sort = dto.Sort,
            IsEditable = dto.IsEditable,
            // 用户新增的配置不是系统配置
            IsSystem = false,
            // 新增配置时设置当前租户信息
            TenantId = currentTenantId,
            TenantCode = currentTenantCode
        };

        await _systemConfigRepository.AddAsync(config);
        var createdConfig = await _systemConfigRepository.GetByIdAsync(config.Id);
        if (createdConfig == null)
        {
            throw new InvalidOperationException("创建配置失败");
        }
        return ApiResponseDto<SystemConfigDto>.Success(createdConfig.Adapt<SystemConfigDto>(), "创建成功");
    }

    public async Task<ApiResponseDto<SystemConfigDto>> UpdateAsync(SystemConfigUpdateDto dto, long currentTenantId, string currentTenantCode, bool isSuperAdmin)
    {
        var config = await _systemConfigRepository.GetByIdAsync(dto.Id);
        if (config == null)
        {
            throw new InvalidOperationException("配置不存在");
        }

        // 超级管理员可编辑所有数据，不受 IsEditable 限制
        // 其他角色仅可编辑 IsEditable=true 的数据
        if (!isSuperAdmin && !config.IsEditable)
        {
            throw new InvalidOperationException("系统内置配置不可修改");
        }

        // 当更新 DefaultPageSize 时，验证值必须在 DefaultPageSizes 列表中
        if (config.ConfigKey.ToLower() == "defaultpagesize")
        {
            var pageSizesConfig = await _systemConfigRepository.GetByKeyAsync("DefaultPageSizes");
            if (pageSizesConfig != null && !string.IsNullOrEmpty(pageSizesConfig.ConfigValue))
            {
                var allowedSizes = pageSizesConfig.ConfigValue.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .ToList();

                if (!allowedSizes.Contains(dto.ConfigValue.Trim()))
                {
                    throw new InvalidOperationException($"DefaultPageSize 的值必须在 DefaultPageSizes 列表中（当前允许值：{pageSizesConfig.ConfigValue}）");
                }
            }
        }

        // 非超级管理员用户编辑配置时：
        // - 编辑自己已有的私有配置（IsPublic=false 且属于当前租户）：直接修改原数据
        // - 编辑其他配置：新增一条私有配置
        if (!isSuperAdmin)
        {
            // 如果是当前租户的私有配置，直接修改原数据
            if (!config.IsPublic && config.TenantId == currentTenantId)
            {
                config.ConfigValue = dto.ConfigValue;
                config.Sort = dto.Sort;
                await _systemConfigRepository.UpdateAsync(config);
                var updatedConfig = await _systemConfigRepository.GetByIdAsync(config.Id);
                if (updatedConfig == null)
                {
                    throw new InvalidOperationException("更新配置失败");
                }
                return ApiResponseDto<SystemConfigDto>.Success(updatedConfig.Adapt<SystemConfigDto>(), "更新成功");
            }

            // 否则新增一条私有配置
            var newConfig = new SystemConfig
            {
                ConfigKey = config.ConfigKey,
                ConfigValue = dto.ConfigValue,
                ConfigGroup = config.ConfigGroup,
                Description = config.Description,
                IsPublic = false,  // 固定为私有配置
                Sort = dto.Sort,
                IsEditable = dto.IsEditable,  // 继承用户设置的 IsEditable 值
                TenantId = currentTenantId,
                TenantCode = currentTenantCode  // 使用当前用户的租户 Code
            };

            await _systemConfigRepository.AddAsync(newConfig);
            var createdConfig = await _systemConfigRepository.GetByIdAsync(newConfig.Id);
            if (createdConfig == null)
            {
                throw new InvalidOperationException("创建私有配置失败");
            }
            return ApiResponseDto<SystemConfigDto>.Success(createdConfig.Adapt<SystemConfigDto>(), "创建成功");
        }

        // 超级管理员直接修改原数据
        config.ConfigValue = dto.ConfigValue;
        config.ConfigGroup = dto.ConfigGroup;
        config.Description = dto.Description;
        config.IsPublic = dto.IsPublic;
        config.IsEditable = dto.IsEditable;
        config.Sort = dto.Sort;

        await _systemConfigRepository.UpdateAsync(config);
        var resultConfig = await _systemConfigRepository.GetByIdAsync(config.Id);
        if (resultConfig == null)
        {
            throw new InvalidOperationException("更新配置失败");
        }
        return ApiResponseDto<SystemConfigDto>.Success(resultConfig.Adapt<SystemConfigDto>(), "更新成功");
    }

    public async Task<ApiResponseDto> DeleteAsync(long id, long currentTenantId, bool isSuperAdmin)
    {
        try
        {
            var config = await _systemConfigRepository.GetByIdAsync(id);
            if (config == null)
            {
                return ApiResponseDto.Fail("配置不存在", 404);
            }

            // 系统配置禁止任何用户删除
            if (config.IsSystem)
            {
                return ApiResponseDto.Fail("系统配置禁止删除", 403);
            }

            // 非超级管理员删除权限校验
            if (!isSuperAdmin)
            {
                // 权限校验：非超级管理员仅可删除自己创建的私有配置
                // 私有配置判定：IsPublic=false 且属于当前租户
                var isOwnPrivateConfig = !config.IsPublic && config.TenantId == currentTenantId;
                if (!isOwnPrivateConfig)
                {
                    return ApiResponseDto.Fail("无权删除此配置", 403);
                }

                // IsEditable=false 的配置不可删除
                if (!config.IsEditable)
                {
                    return ApiResponseDto.Fail("此配置项不可修改/删除", 403);
                }
            }

            await _systemConfigRepository.DeleteAsync(id);
            return ApiResponseDto.Success(null, "删除成功");
        }
        catch (InvalidOperationException ex)
        {
            return ApiResponseDto.Fail(ex.Message, 400);
        }
    }
}
