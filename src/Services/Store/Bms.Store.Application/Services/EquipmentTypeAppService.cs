using Mapster;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Bms.BuildingBlocks.Abstractions.Security;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.EquipmentTypes;
using Bms.Store.Domain.Entities;
using Bms.Store.Infrastructure;

namespace Bms.Store.Application.Services;

/// <summary>
/// 设备类型应用服务实现
/// 设备类型为租户级共享数据（所有门店共用同一套，不按门店隔离）
/// 支持父子级自引用：父级节点作为分类/分组，子级节点为具体型号
/// </summary>
public class EquipmentTypeAppService : IEquipmentTypeAppService
{
    private readonly StoreDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<EquipmentTypeCreateDto> _createValidator;
    private readonly IValidator<EquipmentTypeUpdateDto> _updateValidator;

    public EquipmentTypeAppService(
        StoreDbContext dbContext,
        ICurrentUser currentUser,
        IValidator<EquipmentTypeCreateDto> createValidator,
        IValidator<EquipmentTypeUpdateDto> updateValidator)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<ApiResponseDto<PagedResponseDto<EquipmentTypeDto>>> GetPagedListAsync(EquipmentTypeQueryDto query)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<PagedResponseDto<EquipmentTypeDto>>.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;
        var queryable = _dbContext.EquipmentTypes
            .Where(t => !t.IsDeleted && t.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(query.Name))
            queryable = queryable.Where(t => t.Name.Contains(query.Name));
        if (!string.IsNullOrWhiteSpace(query.Code))
            queryable = queryable.Where(t => t.Code.Contains(query.Code));
        if (query.IsActive.HasValue)
            queryable = queryable.Where(t => t.IsActive == query.IsActive.Value);
        if (query.ParentId.HasValue)
            queryable = queryable.Where(t => t.ParentId == query.ParentId.Value);

        var total = await queryable.CountAsync();
        var items = await queryable
            .OrderByDescending(t => t.CreatedTime)
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var result = new PagedResponseDto<EquipmentTypeDto>
        {
            List = await FillEquipmentCountsAsync(items, tenantId),
            Total = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
        return ApiResponseDto<PagedResponseDto<EquipmentTypeDto>>.Ok(result);
    }

    public async Task<ApiResponseDto<List<EquipmentTypeDto>>> GetAllAsync()
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<List<EquipmentTypeDto>>.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;
        // 下拉选择场景只返回叶子节点（具体型号），排除作为分组的父级节点
        var items = await _dbContext.EquipmentTypes
            .Where(t => !t.IsDeleted && t.TenantId == tenantId && t.IsActive
                && !_dbContext.EquipmentTypes.Any(t2 => t2.ParentId == t.Id && !t2.IsDeleted)
                // 父级分类必须启用（停用分支整体不出现在可选列表）
                && (t.ParentId == null
                    || _dbContext.EquipmentTypes.Any(p => p.Id == t.ParentId.Value && p.TenantId == tenantId && !p.IsDeleted && p.IsActive)))
            .OrderBy(t => t.Name)
            .ToListAsync();

        return ApiResponseDto<List<EquipmentTypeDto>>.Ok(await FillEquipmentCountsAsync(items, tenantId));
    }

    public async Task<ApiResponseDto<List<EquipmentTypeDto>>> GetTreeAsync()
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<List<EquipmentTypeDto>>.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;
        var items = await _dbContext.EquipmentTypes
            .Where(t => !t.IsDeleted && t.TenantId == tenantId)
            .OrderBy(t => t.Id)
            .ToListAsync();

        var dtos = await FillEquipmentCountsAsync(items, tenantId);
        return ApiResponseDto<List<EquipmentTypeDto>>.Ok(BuildTree(dtos));
    }

    public async Task<ApiResponseDto<EquipmentTypeDto?>> GetByIdAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<EquipmentTypeDto?>.Fail("登录状态异常，请重新登录", 401);

        var type = await _dbContext.EquipmentTypes
            .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted && t.TenantId == _currentUser.TenantId.Value);
        if (type == null)
            return ApiResponseDto<EquipmentTypeDto?>.Fail("设备类型不存在", 404);

        var dto = type.Adapt<EquipmentTypeDto>();
        dto.EquipmentCount = await _dbContext.Equipments
            .CountAsync(e => e.EquipmentTypeId == id && e.TenantId == _currentUser.TenantId.Value && !e.IsDeleted);
        if (type.ParentId.HasValue)
        {
            dto.ParentName = await _dbContext.EquipmentTypes
                .Where(t => t.Id == type.ParentId.Value)
                .Select(t => t.Name)
                .FirstOrDefaultAsync();
        }
        return ApiResponseDto<EquipmentTypeDto?>.Ok(dto);
    }

    public async Task<ApiResponseDto<EquipmentTypeDto>> CreateAsync(EquipmentTypeCreateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<EquipmentTypeDto>.Fail("登录状态异常，请重新登录", 401);

        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<EquipmentTypeDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var codeExists = await _dbContext.EquipmentTypes
            .AnyAsync(t => t.Code == dto.Code && t.TenantId == tenantId && !t.IsDeleted);
        if (codeExists)
            return ApiResponseDto<EquipmentTypeDto>.Fail($"类型编码 {dto.Code} 已存在", 400);

        // 父级校验：存在且未删除；已停用的父级禁止新增子级（联动不变量：父停用→子不可用）
        if (dto.ParentId.HasValue)
        {
            var parent = await _dbContext.EquipmentTypes
                .FirstOrDefaultAsync(t => t.Id == dto.ParentId.Value && t.TenantId == tenantId && !t.IsDeleted);
            if (parent == null)
                return ApiResponseDto<EquipmentTypeDto>.Fail("上级分类不存在", 400);
            if (!parent.IsActive)
                return ApiResponseDto<EquipmentTypeDto>.Fail("上级分类已停用，请先启用后再新增子级", 400);
        }

        var type = dto.Adapt<EquipmentType>();
        type.TenantId = tenantId;
        type.TenantCode = _currentUser.TenantCode ?? string.Empty;
        // 设备类型为租户级共享数据，不按门店隔离，StoreId 置 0
        type.StoreId = 0;
        type.StoreCode = string.Empty;
        type.CreatedTime = DateTime.Now;

        _dbContext.EquipmentTypes.Add(type);
        await _dbContext.SaveChangesAsync();

        var resultDto = type.Adapt<EquipmentTypeDto>();
        resultDto.EquipmentCount = 0;
        if (type.ParentId.HasValue)
            resultDto.ParentName = await _dbContext.EquipmentTypes
                .Where(t => t.Id == type.ParentId.Value)
                .Select(t => t.Name)
                .FirstOrDefaultAsync();
        return ApiResponseDto<EquipmentTypeDto>.Ok(resultDto, "创建成功");
    }

    public async Task<ApiResponseDto<EquipmentTypeDto>> UpdateAsync(EquipmentTypeUpdateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<EquipmentTypeDto>.Fail("登录状态异常，请重新登录", 401);

        var validation = await _updateValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<EquipmentTypeDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var type = await _dbContext.EquipmentTypes
            .FirstOrDefaultAsync(t => t.Id == dto.Id && !t.IsDeleted && t.TenantId == tenantId);
        if (type == null)
            return ApiResponseDto<EquipmentTypeDto>.Fail("设备类型不存在", 404);

        if (type.Code != dto.Code)
        {
            var codeExists = await _dbContext.EquipmentTypes
                .AnyAsync(t => t.Code == dto.Code && t.TenantId == tenantId && !t.IsDeleted && t.Id != dto.Id);
            if (codeExists)
                return ApiResponseDto<EquipmentTypeDto>.Fail($"类型编码 {dto.Code} 已存在", 400);
        }

        // 父级校验：不能设自己为父级，父级必须存在、未删除且启用
        if (dto.ParentId.HasValue)
        {
            if (dto.ParentId.Value == dto.Id)
                return ApiResponseDto<EquipmentTypeDto>.Fail("上级分类不能是自己", 400);
            var parent = await _dbContext.EquipmentTypes
                .FirstOrDefaultAsync(t => t.Id == dto.ParentId.Value && t.TenantId == tenantId && !t.IsDeleted);
            if (parent == null)
                return ApiResponseDto<EquipmentTypeDto>.Fail("上级分类不存在", 400);
            if (!parent.IsActive)
                return ApiResponseDto<EquipmentTypeDto>.Fail("上级分类已停用，请先启用后再操作", 400);
        }

        // 启用校验：启用一个节点要求其祖先链全部启用（单向联动不变量，防止在停用分支下单独恢复启用）
        if (dto.IsActive && await HasInactiveAncestorAsync(tenantId, dto.ParentId))
            return ApiResponseDto<EquipmentTypeDto>.Fail("上级分类已停用，请先启用后再启用该类型", 400);

        type.Name = dto.Name;
        type.Code = dto.Code;
        type.ParentId = dto.ParentId;
        type.Spec = dto.Spec;
        type.Description = dto.Description;
        type.IsActive = dto.IsActive;
        type.UpdatedTime = DateTime.Now;

        // 单向联动：父级停用时递归停用其所有子级；重新启用父级不自动恢复子级（需手动逐个启用）
        if (!type.IsActive)
        {
            var descendantIds = await CollectDescendantIdsAsync(tenantId, type.Id);
            if (descendantIds.Count > 0)
            {
                var descendants = await _dbContext.EquipmentTypes
                    .Where(t => descendantIds.Contains(t.Id))
                    .ToListAsync();
                foreach (var descendant in descendants)
                {
                    if (!descendant.IsActive) continue;
                    descendant.IsActive = false;
                    descendant.UpdatedTime = DateTime.Now;
                }
            }
        }

        await _dbContext.SaveChangesAsync();

        var resultDto = type.Adapt<EquipmentTypeDto>();
        resultDto.EquipmentCount = await _dbContext.Equipments
            .CountAsync(e => e.EquipmentTypeId == dto.Id && e.TenantId == tenantId && !e.IsDeleted);
        if (type.ParentId.HasValue)
            resultDto.ParentName = await _dbContext.EquipmentTypes
                .Where(t => t.Id == type.ParentId.Value)
                .Select(t => t.Name)
                .FirstOrDefaultAsync();
        return ApiResponseDto<EquipmentTypeDto>.Ok(resultDto, "更新成功");
    }

    public async Task<ApiResponseDto> DeleteAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;
        var type = await _dbContext.EquipmentTypes
            .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted && t.TenantId == tenantId);
        if (type == null)
            return ApiResponseDto.Fail("设备类型不存在", 404);

        // 删除保护：存在子类型（作为分类被引用）时禁止删除
        var childCount = await _dbContext.EquipmentTypes
            .CountAsync(t => t.ParentId == id && t.TenantId == tenantId && !t.IsDeleted);
        if (childCount > 0)
            return ApiResponseDto.Fail($"该分类下存在 {childCount} 个子类型，请先删除或调整后再删除", 400);

        // 删除保护：被设备实例引用时禁止删除
        var equipmentCount = await _dbContext.Equipments
            .CountAsync(e => e.EquipmentTypeId == id && e.TenantId == tenantId && !e.IsDeleted);
        if (equipmentCount > 0)
            return ApiResponseDto.Fail($"该类型已被 {equipmentCount} 台设备引用，请先在设备档案中调整后再删除", 400);

        // 删除保护：被服务项目关联时禁止删除（关联表不启用软删除）
        var serviceCount = await _dbContext.ServiceProductEquipments
            .CountAsync(r => r.EquipmentTypeId == id && r.TenantId == tenantId);
        if (serviceCount > 0)
            return ApiResponseDto.Fail($"该类型已被 {serviceCount} 个服务项目关联，请先在服务项目中移除后再删除", 400);

        type.IsDeleted = true;
        type.UpdatedTime = DateTime.Now;
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, "删除成功");
    }

    /// <summary>
    /// 递归收集指定节点下所有子级ID（含全部层级，用于停用联动）
    /// </summary>
    private async Task<HashSet<long>> CollectDescendantIdsAsync(long tenantId, long parentId)
    {
        var result = new HashSet<long>();
        var frontier = new List<long> { parentId };
        while (frontier.Count > 0)
        {
            var children = await _dbContext.EquipmentTypes
                .Where(t => t.TenantId == tenantId && !t.IsDeleted
                    && t.ParentId.HasValue && frontier.Contains(t.ParentId!.Value))
                .Select(t => t.Id)
                .ToListAsync();
            result.UnionWith(children);
            frontier = children;
        }
        return result;
    }

    /// <summary>
    /// 从指定父级向上回溯，判断祖先链中是否存在已停用节点
    /// 链路断裂（父级不存在/已删除）视为不可用，返回 true（防御性拦截）
    /// </summary>
    private async Task<bool> HasInactiveAncestorAsync(long tenantId, long? parentId)
    {
        var current = parentId;
        while (current.HasValue)
        {
            var parent = await _dbContext.EquipmentTypes
                .FirstOrDefaultAsync(t => t.Id == current.Value && t.TenantId == tenantId && !t.IsDeleted);
            if (parent == null || !parent.IsActive)
                return true;
            current = parent.ParentId;
        }
        return false;
    }

    /// <summary>
    /// 批量填充设备类型的关联设备数量与父级名称
    /// </summary>
    private async Task<List<EquipmentTypeDto>> FillEquipmentCountsAsync(List<EquipmentType> items, long tenantId)
    {
        var typeIds = items.Select(t => t.Id).ToList();
        var dtos = items.Adapt<List<EquipmentTypeDto>>();

        if (typeIds.Count == 0)
            return dtos;

        var counts = await _dbContext.Equipments
            .Where(e => !e.IsDeleted && e.TenantId == tenantId && typeIds.Contains(e.EquipmentTypeId))
            .GroupBy(e => e.EquipmentTypeId)
            .Select(g => new { EquipmentTypeId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.EquipmentTypeId, x => x.Count);

        foreach (var dto in dtos)
            dto.EquipmentCount = counts.GetValueOrDefault(dto.Id);

        // 填充父级名称（冗余展示字段）
        var parentIds = items.Where(t => t.ParentId.HasValue).Select(t => t.ParentId!.Value).Distinct().ToList();
        if (parentIds.Count > 0)
        {
            var parentNames = await _dbContext.EquipmentTypes
                .Where(t => parentIds.Contains(t.Id))
                .ToDictionaryAsync(t => t.Id, t => t.Name);
            foreach (var dto in dtos)
            {
                if (dto.ParentId.HasValue)
                    dto.ParentName = parentNames.GetValueOrDefault(dto.ParentId.Value);
            }
        }

        return dtos;
    }

    /// <summary>
    /// 构建设备类型树形结构（父级节点为分类/分组）
    /// </summary>
    private static List<EquipmentTypeDto> BuildTree(List<EquipmentTypeDto> types)
    {
        var lookup = types.ToLookup(t => t.ParentId);
        foreach (var type in types)
        {
            type.Children = lookup[type.Id].ToList();
        }
        return types.Where(t => t.ParentId == null).ToList();
    }
}
