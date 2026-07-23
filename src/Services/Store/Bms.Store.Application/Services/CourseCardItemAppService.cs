using Mapster;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Bms.BuildingBlocks.Abstractions.Security;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.TreatmentCards;
using CourseCardItemEntity = Bms.Store.Domain.Entities.CourseCardItem;
using Bms.Store.Infrastructure;

namespace Bms.Store.Application.Services;

/// <summary>
/// 疗程卡项目关联应用服务实现
/// </summary>
public class CourseCardItemAppService : ICourseCardItemAppService
{
    private readonly StoreDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<CourseCardItemCreateDto> _createValidator;
    private readonly IValidator<CourseCardItemUpdateDto> _updateValidator;

    public CourseCardItemAppService(
        StoreDbContext dbContext,
        ICurrentUser currentUser,
        IValidator<CourseCardItemCreateDto> createValidator,
        IValidator<CourseCardItemUpdateDto> updateValidator)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>
    /// 获取疗程卡项目关联分页列表
    /// </summary>
    public async Task<ApiResponseDto<PagedResponseDto<CourseCardItemDto>>> GetPagedListAsync(CourseCardItemQueryDto query)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<PagedResponseDto<CourseCardItemDto>>.Fail("无法确定当前租户", 401);

        var tenantId = _currentUser.TenantId.Value;
        var queryable = _dbContext.CourseCardItems
            .Where(c => !c.IsDeleted && c.TenantId == tenantId);

        if (query.CourseCardId.HasValue)
            queryable = queryable.Where(c => c.CourseCardId == query.CourseCardId.Value);

        var total = await queryable.CountAsync();
        var items = await queryable
            .OrderByDescending(c => c.CreatedTime)
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var result = new PagedResponseDto<CourseCardItemDto>
        {
            List = items.Adapt<List<CourseCardItemDto>>(),
            Total = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
        return ApiResponseDto<PagedResponseDto<CourseCardItemDto>>.Ok(result);
    }

    /// <summary>
    /// 根据ID获取疗程卡项目关联详情
    /// </summary>
    public async Task<ApiResponseDto<CourseCardItemDto?>> GetByIdAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<CourseCardItemDto?>.Fail("无法确定当前租户", 401);

        var entity = await _dbContext.CourseCardItems
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted && c.TenantId == _currentUser.TenantId.Value);
        if (entity == null)
            return ApiResponseDto<CourseCardItemDto?>.Fail("疗程卡项目关联不存在", 404);
        return ApiResponseDto<CourseCardItemDto?>.Ok(entity.Adapt<CourseCardItemDto>());
    }

    /// <summary>
    /// 创建疗程卡项目关联
    /// </summary>
    public async Task<ApiResponseDto<CourseCardItemDto>> CreateAsync(CourseCardItemCreateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<CourseCardItemDto>.Fail("无法确定当前租户", 401);

        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<CourseCardItemDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var entity = dto.Adapt<CourseCardItemEntity>();
        entity.TenantId = tenantId;
        entity.TenantCode = _currentUser.TenantCode ?? string.Empty;
        entity.CreatedTime = DateTime.Now;

        _dbContext.CourseCardItems.Add(entity);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto<CourseCardItemDto>.Ok(entity.Adapt<CourseCardItemDto>(), "创建成功");
    }

    /// <summary>
    /// 更新疗程卡项目关联
    /// </summary>
    public async Task<ApiResponseDto<CourseCardItemDto>> UpdateAsync(CourseCardItemUpdateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<CourseCardItemDto>.Fail("无法确定当前租户", 401);

        var validation = await _updateValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<CourseCardItemDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var entity = await _dbContext.CourseCardItems
            .FirstOrDefaultAsync(c => c.Id == dto.Id && !c.IsDeleted && c.TenantId == tenantId);
        if (entity == null)
            return ApiResponseDto<CourseCardItemDto>.Fail("疗程卡项目关联不存在", 404);

        entity.ProductId = dto.ProductId;
        entity.Quantity = dto.Quantity;
        entity.OriginalPrice = dto.OriginalPrice;
        entity.UpdatedTime = DateTime.Now;

        await _dbContext.SaveChangesAsync();
        return ApiResponseDto<CourseCardItemDto>.Ok(entity.Adapt<CourseCardItemDto>(), "更新成功");
    }

    /// <summary>
    /// 删除疗程卡项目关联（软删除）
    /// </summary>
    public async Task<ApiResponseDto> DeleteAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("无法确定当前租户", 401);

        var entity = await _dbContext.CourseCardItems
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted && c.TenantId == _currentUser.TenantId.Value);
        if (entity == null)
            return ApiResponseDto.Fail("疗程卡项目关联不存在", 404);

        entity.IsDeleted = true;
        entity.UpdatedTime = DateTime.Now;
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, "删除成功");
    }

    /// <summary>
    /// 批量删除疗程卡项目关联（软删除）
    /// </summary>
    public async Task<ApiResponseDto> BatchDeleteAsync(List<long> ids)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("无法确定当前租户", 401);
        if (ids == null || !ids.Any())
            return ApiResponseDto.Fail("请选择要删除的数据", 400);

        var entities = await _dbContext.CourseCardItems
            .Where(c => ids.Contains(c.Id) && !c.IsDeleted && c.TenantId == _currentUser.TenantId.Value)
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
