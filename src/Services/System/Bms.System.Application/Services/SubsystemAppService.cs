using Mapster;
using Bms.System.Application.Dtos;
using Bms.System.Application.Dtos.Subsystems;
using Bms.System.Domain.Entities;
using Bms.System.Domain.IRepositories;

namespace Bms.System.Application.Services;

/// <summary>
/// 子系统应用服务实现
/// </summary>
public class SubsystemAppService : ISubsystemAppService
{
    private readonly ISubsystemRepository _subsystemRepository;
    private readonly ISubsystemMenuRepository _subsystemMenuRepository;
    private readonly ITenantSubsystemRepository _tenantSubsystemRepository;
    private readonly ITenantSubsystemAppService _tenantSubsystemAppService;

    /// <summary>
    /// 平台租户ID
    /// </summary>
    private const long PlatformTenantId = 1;

    public SubsystemAppService(
        ISubsystemRepository subsystemRepository,
        ISubsystemMenuRepository subsystemMenuRepository,
        ITenantSubsystemRepository tenantSubsystemRepository,
        ITenantSubsystemAppService tenantSubsystemAppService)
    {
        _subsystemRepository = subsystemRepository;
        _subsystemMenuRepository = subsystemMenuRepository;
        _tenantSubsystemRepository = tenantSubsystemRepository;
        _tenantSubsystemAppService = tenantSubsystemAppService;
    }

    public async Task<ApiResponseDto<List<SubsystemDto>>> GetListAsync(SubsystemQueryDto query, bool isSuperAdmin = true, long? tenantId = null)
    {
        var list = await _subsystemRepository.GetListAsync();

        // 租户隔离：所有用户都只能看到分配给当前租户的子系统
        // 超级管理员的权限体现在管理视角（GetAllAsync），导航栏显示受租户限制
        if (tenantId.HasValue)
        {
            var tenantSubsystems = await _tenantSubsystemRepository.GetByTenantIdAsync(tenantId.Value);
            var allowedSubsystemIds = tenantSubsystems.Select(ts => ts.SubsystemId).ToHashSet();
            list = list.Where(x => allowedSubsystemIds.Contains(x.Id)).ToList();
        }

        // 筛选（忽略大小写）
        if (!string.IsNullOrEmpty(query.Name))
        {
            list = list.Where(x => x.Name.ToLower().Contains(query.Name.ToLower())).ToList();
        }

        if (!string.IsNullOrEmpty(query.Code))
        {
            list = list.Where(x => x.Code.ToLower().Contains(query.Code.ToLower())).ToList();
        }

        if (query.Status.HasValue)
        {
            list = list.Where(x => x.Status == query.Status.Value).ToList();
        }

        var dtos = list.Select(s => s.Adapt<SubsystemDto>()).ToList();
        return ApiResponseDto<List<SubsystemDto>>.Success(dtos);
    }

    public async Task<ApiResponseDto<List<SubsystemDto>>> GetAllEnabledAsync(bool isSuperAdmin = true, long? tenantId = null)
    {
        var list = await _subsystemRepository.GetListAsync();

        // 租户隔离：所有用户都只能看到分配给当前租户的子系统
        if (tenantId.HasValue)
        {
            var tenantSubsystems = await _tenantSubsystemRepository.GetByTenantIdAsync(tenantId.Value);
            var allowedSubsystemIds = tenantSubsystems.Select(ts => ts.SubsystemId).ToHashSet();
            list = list.Where(x => allowedSubsystemIds.Contains(x.Id)).ToList();
        }

        var enabledList = list.Where(x => x.Status == 1).ToList();
        var dtos = enabledList.Select(s => s.Adapt<SubsystemDto>()).ToList();
        return ApiResponseDto<List<SubsystemDto>>.Success(dtos);
    }

    /// <summary>
    /// 获取所有子系统（不分启用/禁用状态）
    /// </summary>
    public async Task<ApiResponseDto<List<SubsystemDto>>> GetAllAsync(bool isSuperAdmin = true, long? tenantId = null)
    {
        var list = await _subsystemRepository.GetListAsync();

        // 超级管理员可以查看所有子系统，用于租户管理
        // 其他用户受租户隔离限制
        if (!isSuperAdmin && tenantId.HasValue)
        {
            var tenantSubsystems = await _tenantSubsystemRepository.GetByTenantIdAsync(tenantId.Value);
            var allowedSubsystemIds = tenantSubsystems.Select(ts => ts.SubsystemId).ToHashSet();
            list = list.Where(x => allowedSubsystemIds.Contains(x.Id)).ToList();
        }

        var dtos = list.Select(s => s.Adapt<SubsystemDto>()).ToList();
        return ApiResponseDto<List<SubsystemDto>>.Success(dtos);
    }

    public async Task<ApiResponseDto<SubsystemDto?>> GetByIdAsync(long id)
    {
        var entity = await _subsystemRepository.GetByIdAsync(id);
        if (entity == null)
        {
            return ApiResponseDto<SubsystemDto?>.Fail("子系统不存在", 404);
        }

        return ApiResponseDto<SubsystemDto?>.Success(entity.Adapt<SubsystemDto>());
    }

    public async Task<ApiResponseDto<int>> GetUsageCountAsync(long id)
    {
        var count = await _subsystemRepository.GetUsageCountAsync(id);
        return ApiResponseDto<int>.Success(count);
    }

    public async Task<ApiResponseDto<SubsystemDto>> CreateAsync(SubsystemCreateDto dto)
    {
        if (await _subsystemRepository.ExistsCodeAsync(dto.Code))
        {
            throw new InvalidOperationException($"子系统编码 {dto.Code} 已存在");
        }

        var entity = new Subsystem
        {
            Code = dto.Code,
            Name = dto.Name,
            Icon = dto.Icon,
            Description = dto.Description,
            Sort = dto.Sort,
            Status = dto.Status,
            CreatedTime = DateTime.Now,
            UpdatedTime = DateTime.Now
        };

        await _subsystemRepository.AddAsync(entity);

        var createdEntity = await _subsystemRepository.GetByIdAsync(entity.Id);
        if (createdEntity == null)
        {
            throw new InvalidOperationException("创建子系统失败");
        }

        // 自动将新子系统分配给平台租户（仅创建关联记录，不分配菜单权限）
        await _tenantSubsystemAppService.AddSubsystemAsync(PlatformTenantId, createdEntity.Id);

        return ApiResponseDto<SubsystemDto>.Success(createdEntity.Adapt<SubsystemDto>(), "创建成功");
    }

    public async Task<ApiResponseDto<SubsystemDto>> UpdateAsync(SubsystemUpdateDto dto)
    {
        var entity = await _subsystemRepository.GetByIdAsync(dto.Id);
        if (entity == null)
        {
            throw new InvalidOperationException("子系统不存在");
        }

        entity.Name = dto.Name;
        entity.Icon = dto.Icon;
        entity.Description = dto.Description;
        entity.Sort = dto.Sort;
        entity.Status = dto.Status;
        entity.UpdatedTime = DateTime.Now;

        await _subsystemRepository.UpdateAsync(entity);

        var updatedEntity = await _subsystemRepository.GetByIdAsync(dto.Id);
        if (updatedEntity == null)
        {
            throw new InvalidOperationException("更新子系统失败");
        }

        return ApiResponseDto<SubsystemDto>.Success(updatedEntity.Adapt<SubsystemDto>(), "更新成功");
    }

    public async Task<ApiResponseDto> DeleteAsync(long id)
    {
        var usageCount = await _subsystemRepository.GetUsageCountAsync(id);
        if (usageCount > 0)
        {
            throw new InvalidOperationException($"该子系统已被 {usageCount} 个租户使用，无法删除");
        }

        await _subsystemMenuRepository.DeleteBySubsystemIdAsync(id);
        await _subsystemRepository.DeleteAsync(id);

        return ApiResponseDto.Success(null, "删除成功");
    }

    public async Task<ApiResponseDto<List<long>>> GetMenusAsync(long id)
    {
        var subsystemMenus = await _subsystemMenuRepository.GetBySubsystemIdAsync(id);
        var menuIds = subsystemMenus.Select(x => x.MenuId).ToList();
        return ApiResponseDto<List<long>>.Success(menuIds);
    }

    public async Task<ApiResponseDto> AssignMenusAsync(long id, SubsystemMenuAssignDto dto)
    {
        // 先去重，避免重复键错误
        var menuIds = dto.MenuIds.Distinct().ToList();

        // 使用统一的事务方法，先删后增
        await _subsystemMenuRepository.ReplaceAllAsync(id, menuIds);

        return ApiResponseDto.Success(null, "分配成功");
    }
}
