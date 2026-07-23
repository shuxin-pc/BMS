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
            return ApiResponseDto<PagedResponseDto<SkillCategoryDto>>.Fail("无法确定当前租户", 401);

        var tenantId = _currentUser.TenantId.Value;
        var queryable = _dbContext.SkillCategories
            .Where(s => !s.IsDeleted && s.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(query.Name))
            queryable = queryable.Where(s => s.Name.Contains(query.Name));
        if (!string.IsNullOrWhiteSpace(query.Code))
            queryable = queryable.Where(s => s.Code.Contains(query.Code));
        if (query.ParentId.HasValue)
            queryable = queryable.Where(s => s.ParentId == query.ParentId.Value);
        if (query.Status.HasValue)
            queryable = queryable.Where(s => s.Status == query.Status.Value);

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
            return ApiResponseDto<SkillCategoryDto?>.Fail("无法确定当前租户", 401);

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
            return ApiResponseDto<SkillCategoryDto>.Fail("无法确定当前租户", 401);

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
            return ApiResponseDto<SkillCategoryDto>.Fail("无法确定当前租户", 401);

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
        entity.Status = dto.Status;
        entity.Remark = dto.Remark;
        entity.UpdatedTime = DateTime.Now;

        await _dbContext.SaveChangesAsync();
        return ApiResponseDto<SkillCategoryDto>.Ok(entity.Adapt<SkillCategoryDto>(), "更新成功");
    }

    /// <summary>
    /// 删除技能分类（软删除）
    /// </summary>
    public async Task<ApiResponseDto> DeleteAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("无法确定当前租户", 401);

        var entity = await _dbContext.SkillCategories
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted && s.TenantId == _currentUser.TenantId.Value);
        if (entity == null)
            return ApiResponseDto.Fail("技能分类不存在", 404);

        entity.IsDeleted = true;
        entity.UpdatedTime = DateTime.Now;
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, "删除成功");
    }

    /// <summary>
    /// 批量删除技能分类（软删除）
    /// </summary>
    public async Task<ApiResponseDto> BatchDeleteAsync(List<long> ids)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("无法确定当前租户", 401);
        if (ids == null || !ids.Any())
            return ApiResponseDto.Fail("请选择要删除的数据", 400);

        var entities = await _dbContext.SkillCategories
            .Where(s => ids.Contains(s.Id) && !s.IsDeleted && s.TenantId == _currentUser.TenantId.Value)
            .ToListAsync();

        foreach (var entity in entities)
        {
            entity.IsDeleted = true;
            entity.UpdatedTime = DateTime.Now;
        }
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, $"成功删除 {entities.Count} 条数据");
    }
}
