using Mapster;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Bms.BuildingBlocks.Abstractions.Security;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.SkillCategories;
using SkillCategoryEntity = Bms.Store.Domain.Entities.SkillCategory;
using Bms.Store.Infrastructure;

namespace Bms.Store.Application.Services;

/// <summary>
/// 技能分类管理应用服务实现
/// </summary>
public class SkillCategoryAppService : ISkillCategoryAppService
{
    private readonly StoreDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<SkillCategoryCreateDto> _createValidator;
    private readonly IValidator<SkillCategoryUpdateDto> _updateValidator;

    public SkillCategoryAppService(
        StoreDbContext dbContext,
        ICurrentUser currentUser,
        IValidator<SkillCategoryCreateDto> createValidator,
        IValidator<SkillCategoryUpdateDto> updateValidator)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>
    /// 获取技能分类分页列表
    /// </summary>
    public async Task<ApiResponseDto<PagedResponseDto<SkillCategoryDto>>> GetPagedListAsync(SkillCategoryQueryDto query)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<PagedResponseDto<SkillCategoryDto>>.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;
        var queryable = _dbContext.SkillCategories
            .Where(s => !s.IsDeleted && s.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(query.Name))
            queryable = queryable.Where(s => s.Name.Contains(query.Name));
        if (!string.IsNullOrWhiteSpace(query.Code))
            queryable = queryable.Where(s => s.Code.Contains(query.Code));
        if (query.ParentId.HasValue)
            queryable = queryable.Where(s => s.ParentId == query.ParentId.Value);

        var total = await queryable.CountAsync();
        var items = await queryable
            .OrderByDescending(s => s.CreatedTime)
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var result = new PagedResponseDto<SkillCategoryDto>
        {
            List = items.Adapt<List<SkillCategoryDto>>(),
            Total = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
        return ApiResponseDto<PagedResponseDto<SkillCategoryDto>>.Ok(result);
    }

    /// <summary>
    /// 根据ID获取技能分类详情
    /// </summary>
    public async Task<ApiResponseDto<SkillCategoryDto?>> GetByIdAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<SkillCategoryDto?>.Fail("登录状态异常，请重新登录", 401);

        var entity = await _dbContext.SkillCategories
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted && s.TenantId == _currentUser.TenantId.Value);
        if (entity == null)
            return ApiResponseDto<SkillCategoryDto?>.Fail("技能分类不存在", 404);
        return ApiResponseDto<SkillCategoryDto?>.Ok(entity.Adapt<SkillCategoryDto>());
    }

    /// <summary>
    /// 创建技能分类
    /// </summary>
    public async Task<ApiResponseDto<SkillCategoryDto>> CreateAsync(SkillCategoryCreateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<SkillCategoryDto>.Fail("登录状态异常，请重新登录", 401);

        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<SkillCategoryDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var codeExists = await _dbContext.SkillCategories
            .AnyAsync(s => s.Code == dto.Code && s.TenantId == tenantId && !s.IsDeleted);
        if (codeExists)
            return ApiResponseDto<SkillCategoryDto>.Fail($"编码 {dto.Code} 已存在", 400);

        var entity = dto.Adapt<SkillCategoryEntity>();
        entity.TenantId = tenantId;
        entity.TenantCode = _currentUser.TenantCode ?? string.Empty;
        entity.CreatedTime = DateTime.Now;

        _dbContext.SkillCategories.Add(entity);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto<SkillCategoryDto>.Ok(entity.Adapt<SkillCategoryDto>(), "创建成功");
    }

    /// <summary>
    /// 更新技能分类
    /// </summary>
    public async Task<ApiResponseDto<SkillCategoryDto>> UpdateAsync(SkillCategoryUpdateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<SkillCategoryDto>.Fail("登录状态异常，请重新登录", 401);

        var validation = await _updateValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<SkillCategoryDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var entity = await _dbContext.SkillCategories
            .FirstOrDefaultAsync(s => s.Id == dto.Id && !s.IsDeleted && s.TenantId == tenantId);
        if (entity == null)
            return ApiResponseDto<SkillCategoryDto>.Fail("技能分类不存在", 404);

        if (entity.Code != dto.Code)
        {
            var codeExists = await _dbContext.SkillCategories
                .AnyAsync(s => s.Code == dto.Code && s.TenantId == tenantId && !s.IsDeleted && s.Id != dto.Id);
            if (codeExists)
                return ApiResponseDto<SkillCategoryDto>.Fail($"编码 {dto.Code} 已存在", 400);
        }

        entity.Name = dto.Name;
        entity.Code = dto.Code;
        entity.ParentId = dto.ParentId;
        entity.UpdatedTime = DateTime.Now;

        await _dbContext.SaveChangesAsync();
        return ApiResponseDto<SkillCategoryDto>.Ok(entity.Adapt<SkillCategoryDto>(), "更新成功");
    }

    /// <summary>
    /// 删除技能分类（级联软删除整棵子树，并物理清理关联数据）
    /// 关联对象（技师技能标签、服务项目适用技能）分散在各档案中难以逐一解除，
    /// 故删除时连带处理：软删全部后代分类 + 物理删除 TechnicianSkill / ServiceProductSkill 关联记录
    /// </summary>
    public async Task<ApiResponseDto> DeleteAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;
        var entity = await _dbContext.SkillCategories
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted && s.TenantId == tenantId);
        if (entity == null)
            return ApiResponseDto.Fail("技能分类不存在", 404);

        // 收集整棵子树（含自身）ID，用于级联删除
        var categoryIds = await CollectSubtreeIdsAsync(tenantId, id);

        var result = await CascadeDeleteAsync(tenantId, categoryIds);
        var childCount = result.DeletedCategories - 1;
        return childCount > 0 || result.DeletedRelations > 0
            ? ApiResponseDto.Success(null, $"删除成功，连带删除 {childCount} 个子分类、{result.DeletedRelations} 条关联数据")
            : ApiResponseDto.Success(null, "删除成功");
    }

    /// <summary>
    /// 批量删除技能分类（级联软删除整棵子树，并物理清理关联数据）
    /// 批量选择的分类存在祖孙关系时，子树 ID 自动去重合并
    /// </summary>
    public async Task<ApiResponseDto> BatchDeleteAsync(List<long> ids)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("登录状态异常，请重新登录", 401);
        if (ids == null || !ids.Any())
            return ApiResponseDto.Fail("请选择要删除的数据", 400);

        var tenantId = _currentUser.TenantId.Value;
        var entities = await _dbContext.SkillCategories
            .Where(s => ids.Contains(s.Id) && !s.IsDeleted && s.TenantId == tenantId)
            .ToListAsync();

        // 对每个选中的分类收集整棵子树并合并（重复的后代分类仅处理一次）
        var categoryIds = new HashSet<long>();
        foreach (var entity in entities)
            categoryIds.UnionWith(await CollectSubtreeIdsAsync(tenantId, entity.Id));

        var result = await CascadeDeleteAsync(tenantId, categoryIds);
        // 按去重后的实际删除分类数统计（批量选中存在祖孙关系时 entities.Count 会重复计数）
        return ApiResponseDto.Success(null, $"成功删除 {result.DeletedCategories} 条分类数据，连带 {result.DeletedRelations} 条关联数据");
    }

    /// <summary>
    /// 递归收集指定分类及其全部后代分类ID（含所有层级，用于级联删除）
    /// </summary>
    private async Task<HashSet<long>> CollectSubtreeIdsAsync(long tenantId, long rootId)
    {
        var result = new HashSet<long> { rootId };
        var frontier = new List<long> { rootId };
        while (frontier.Count > 0)
        {
            var children = await _dbContext.SkillCategories
                .Where(s => s.TenantId == tenantId && !s.IsDeleted
                    && s.ParentId.HasValue && frontier.Contains(s.ParentId!.Value))
                .Select(s => s.Id)
                .ToListAsync();
            // 防环：仅将新收集的节点作为下一层 frontier，历史脏数据中的循环引用会自然收敛
            frontier = new List<long>();
            foreach (var child in children)
            {
                if (result.Add(child))
                    frontier.Add(child);
            }
        }
        return result;
    }

    /// <summary>
    /// 执行级联删除：软删子树全部分类 + 物理删除技师技能标签与服务项目适用技能关联记录
    /// </summary>
    private async Task<(int DeletedCategories, int DeletedRelations)> CascadeDeleteAsync(long tenantId, HashSet<long> categoryIds)
    {
        // 软删整棵子树分类（技能分类为核心表，走软删除）
        var categories = await _dbContext.SkillCategories
            .Where(s => categoryIds.Contains(s.Id) && s.TenantId == tenantId)
            .ToListAsync();
        foreach (var category in categories)
        {
            category.IsDeleted = true;
            category.UpdatedTime = DateTime.Now;
        }

        // 物理删除关联数据（关联表不启用软删除，与项目惯例一致）
        var technicianSkills = await _dbContext.TechnicianSkills
            .Where(t => t.TenantId == tenantId && categoryIds.Contains(t.SkillCategoryId))
            .ToListAsync();
        _dbContext.TechnicianSkills.RemoveRange(technicianSkills);

        var serviceProductSkills = await _dbContext.ServiceProductSkills
            .Where(r => r.TenantId == tenantId && categoryIds.Contains(r.SkillCategoryId))
            .ToListAsync();
        _dbContext.ServiceProductSkills.RemoveRange(serviceProductSkills);

        await _dbContext.SaveChangesAsync();
        return (categories.Count, technicianSkills.Count + serviceProductSkills.Count);
    }
}
