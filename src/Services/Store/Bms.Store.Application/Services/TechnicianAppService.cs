using Mapster;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Bms.BuildingBlocks.Abstractions.Security;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Technicians;
using TechnicianEntity = Bms.Store.Domain.Entities.Technician;
using Bms.Store.Domain.Entities;
using Bms.Store.Infrastructure;

namespace Bms.Store.Application.Services;

/// <summary>
/// 商家技师应用服务实现
/// </summary>
public class TechnicianAppService : ITechnicianAppService
{
    /// <summary>
    /// 平台租户ID（平台技师归属于此租户，商家门店可只读选择平台技师）
    /// </summary>
    private const long PlatformTenantId = 1;

    private readonly StoreDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<TechnicianCreateDto> _createValidator;
    private readonly IValidator<TechnicianUpdateDto> _updateValidator;
    private readonly ITechnicianPermissionService _permissionService;

    public TechnicianAppService(
        StoreDbContext dbContext,
        ICurrentUser currentUser,
        IValidator<TechnicianCreateDto> createValidator,
        IValidator<TechnicianUpdateDto> updateValidator,
        ITechnicianPermissionService permissionService)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _permissionService = permissionService;
    }

    /// <summary>
    /// 获取商家技师分页列表
    /// </summary>
    public async Task<ApiResponseDto<PagedResponseDto<TechnicianDto>>> GetPagedListAsync(TechnicianQueryDto query)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<PagedResponseDto<TechnicianDto>>.Fail("无法确定当前租户", 401);

        var tenantId = _currentUser.TenantId.Value;
        // 商家门店可查询本租户技师 + 平台技师（只读选择）；平台租户仅查自己的技师
        var queryable = _dbContext.Technicians
            .Include(t => t.TechnicianSkills).ThenInclude(ts => ts.SkillCategory)
            .Where(t => !t.IsDeleted && (t.TenantId == tenantId || t.TenantId == PlatformTenantId));

        if (!string.IsNullOrWhiteSpace(query.Name))
            queryable = queryable.Where(t => t.Name.Contains(query.Name));
        if (!string.IsNullOrWhiteSpace(query.Phone))
            queryable = queryable.Where(t => t.Phone.Contains(query.Phone));
        if (query.Status.HasValue)
            queryable = queryable.Where(t => t.Status == query.Status.Value);
        if (query.Source.HasValue)
            queryable = queryable.Where(t => t.Source == query.Source.Value);

        var total = await queryable.CountAsync();
        var items = await queryable
            .OrderByDescending(t => t.CreatedTime)
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var result = new PagedResponseDto<TechnicianDto>
        {
            List = items.Adapt<List<TechnicianDto>>(),
            Total = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
        return ApiResponseDto<PagedResponseDto<TechnicianDto>>.Ok(result);
    }

    /// <summary>
    /// 根据ID获取商家技师详情
    /// </summary>
    public async Task<ApiResponseDto<TechnicianDto?>> GetByIdAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<TechnicianDto?>.Fail("无法确定当前租户", 401);

        // 商家门店可查询本租户技师 + 平台技师（只读选择）
        var entity = await _dbContext.Technicians
            .Include(t => t.TechnicianSkills).ThenInclude(ts => ts.SkillCategory)
            .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted
                && (t.TenantId == _currentUser.TenantId.Value || t.TenantId == PlatformTenantId));
        if (entity == null)
            return ApiResponseDto<TechnicianDto?>.Fail("技师不存在", 404);
        return ApiResponseDto<TechnicianDto?>.Ok(entity.Adapt<TechnicianDto>());
    }

    /// <summary>
    /// 创建商家技师
    /// </summary>
    public async Task<ApiResponseDto<TechnicianDto>> CreateAsync(TechnicianCreateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<TechnicianDto>.Fail("无法确定当前租户", 401);

        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<TechnicianDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var phoneExists = await _dbContext.Technicians
            .AnyAsync(t => t.Phone == dto.Phone && t.TenantId == tenantId && !t.IsDeleted);
        if (phoneExists)
            return ApiResponseDto<TechnicianDto>.Fail($"手机号 {dto.Phone} 已存在", 400);

        // 校验技能分类归属本租户（外键约束保证标签值在本租户分类范围内）
        if (dto.SkillCategoryIds != null && dto.SkillCategoryIds.Count > 0)
        {
            var validCategoryIds = await _dbContext.SkillCategories
                .Where(c => !c.IsDeleted && c.TenantId == tenantId
                    && dto.SkillCategoryIds.Contains(c.Id))
                .Select(c => c.Id)
                .ToListAsync();
            var invalidIds = dto.SkillCategoryIds.Except(validCategoryIds).ToList();
            if (invalidIds.Count > 0)
                return ApiResponseDto<TechnicianDto>.Fail($"技能分类ID {string.Join(",", invalidIds)} 不属于当前租户", 400);
        }

        var entity = dto.Adapt<TechnicianEntity>();
        entity.TenantId = tenantId;
        entity.TenantCode = _currentUser.TenantCode ?? string.Empty;
        // 强制根据当前租户设置 Source，忽略 DTO 传入（DTO 已移除 Source 字段）
        // 平台租户创建的为平台技师(2)，商家租户创建的为商家技师(1)
        entity.Source = _permissionService.IsPlatformTenant(tenantId) ? 2 : 1;
        entity.CreatedTime = DateTime.Now;

        // 级联创建技能标签关联
        if (dto.SkillCategoryIds != null && dto.SkillCategoryIds.Count > 0)
        {
            foreach (var categoryId in dto.SkillCategoryIds)
            {
                entity.TechnicianSkills.Add(new TechnicianSkill
                {
                    SkillCategoryId = categoryId,
                    TenantId = tenantId,
                    TenantCode = _currentUser.TenantCode ?? string.Empty,
                    CreatedTime = DateTime.Now
                });
            }
        }

        _dbContext.Technicians.Add(entity);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto<TechnicianDto>.Ok(entity.Adapt<TechnicianDto>(), "创建成功");
    }

    /// <summary>
    /// 更新商家技师
    /// </summary>
    public async Task<ApiResponseDto<TechnicianDto>> UpdateAsync(TechnicianUpdateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<TechnicianDto>.Fail("无法确定当前租户", 401);

        var validation = await _updateValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<TechnicianDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        // TenantId 隔离已保证商家租户无法操作平台技师（平台技师 TenantId=PlatformTenantId=1）
        // 此处无需追加 Source 过滤，TenantId 校验为最终防线
        var entity = await _dbContext.Technicians
            .Include(t => t.TechnicianSkills)
            .FirstOrDefaultAsync(t => t.Id == dto.Id && !t.IsDeleted && t.TenantId == tenantId);
        if (entity == null)
            return ApiResponseDto<TechnicianDto>.Fail("技师不存在", 404);

        // 手机号变更时检查唯一性
        if (entity.Phone != dto.Phone)
        {
            var phoneExists = await _dbContext.Technicians
                .AnyAsync(t => t.Phone == dto.Phone && t.TenantId == tenantId && !t.IsDeleted && t.Id != dto.Id);
            if (phoneExists)
                return ApiResponseDto<TechnicianDto>.Fail($"手机号 {dto.Phone} 已存在", 400);
        }

        // 校验技能分类归属本租户（外键约束保证标签值在本租户分类范围内）
        if (dto.SkillCategoryIds != null && dto.SkillCategoryIds.Count > 0)
        {
            var validCategoryIds = await _dbContext.SkillCategories
                .Where(c => !c.IsDeleted && c.TenantId == tenantId
                    && dto.SkillCategoryIds.Contains(c.Id))
                .Select(c => c.Id)
                .ToListAsync();
            var invalidIds = dto.SkillCategoryIds.Except(validCategoryIds).ToList();
            if (invalidIds.Count > 0)
                return ApiResponseDto<TechnicianDto>.Fail($"技能分类ID {string.Join(",", invalidIds)} 不属于当前租户", 400);
        }

        entity.Name = dto.Name;
        entity.Phone = dto.Phone;
        entity.Gender = dto.Gender;
        entity.AvatarUrl = dto.AvatarUrl;
        entity.Status = dto.Status;
        // Source 字段一旦创建不可变更（UpdateDto 已移除 Source 字段）
        entity.Remark = dto.Remark;
        entity.UpdatedTime = DateTime.Now;

        // 更新技能标签关联（先删旧再建新）
        _dbContext.TechnicianSkills.RemoveRange(entity.TechnicianSkills);
        if (dto.SkillCategoryIds != null && dto.SkillCategoryIds.Count > 0)
        {
            foreach (var categoryId in dto.SkillCategoryIds)
            {
                entity.TechnicianSkills.Add(new TechnicianSkill
                {
                    SkillCategoryId = categoryId,
                    TenantId = tenantId,
                    TenantCode = _currentUser.TenantCode ?? string.Empty,
                    CreatedTime = DateTime.Now
                });
            }
        }

        await _dbContext.SaveChangesAsync();
        return ApiResponseDto<TechnicianDto>.Ok(entity.Adapt<TechnicianDto>(), "更新成功");
    }

    /// <summary>
    /// 删除商家技师（软删除）
    /// </summary>
    public async Task<ApiResponseDto> DeleteAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("无法确定当前租户", 401);

        var tenantId = _currentUser.TenantId.Value;
        // TenantId 隔离已保证商家租户无法操作平台技师（平台技师 TenantId=PlatformTenantId=1）
        // 此处无需追加 Source 过滤，TenantId 校验为最终防线
        var entity = await _dbContext.Technicians
            .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted && t.TenantId == tenantId);
        if (entity == null)
            return ApiResponseDto.Fail("技师不存在", 404);

        entity.IsDeleted = true;
        entity.UpdatedTime = DateTime.Now;
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, "删除成功");
    }

    /// <summary>
    /// 批量删除商家技师（软删除）
    /// </summary>
    public async Task<ApiResponseDto> BatchDeleteAsync(List<long> ids)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("无法确定当前租户", 401);
        if (ids == null || !ids.Any())
            return ApiResponseDto.Fail("请选择要删除的数据", 400);

        var tenantId = _currentUser.TenantId.Value;
        // TenantId 隔离已保证商家租户无法操作平台技师（平台技师 TenantId=PlatformTenantId=1）
        // 批量删除同样无需追加 Source 过滤，TenantId 校验为最终防线
        var entities = await _dbContext.Technicians
            .Where(t => ids.Contains(t.Id) && !t.IsDeleted && t.TenantId == tenantId)
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
