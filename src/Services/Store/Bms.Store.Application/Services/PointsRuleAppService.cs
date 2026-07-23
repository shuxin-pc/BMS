using Mapster;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Bms.BuildingBlocks.Abstractions.Security;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.PointsRules;
using PointsRuleEntity = Bms.Store.Domain.Entities.PointsRule;
using Bms.Store.Infrastructure;

namespace Bms.Store.Application.Services;

/// <summary>
/// 积分规则应用服务实现
/// </summary>
public class PointsRuleAppService : IPointsRuleAppService
{
    private readonly StoreDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<PointsRuleCreateDto> _createValidator;
    private readonly IValidator<PointsRuleUpdateDto> _updateValidator;

    public PointsRuleAppService(
        StoreDbContext dbContext,
        ICurrentUser currentUser,
        IValidator<PointsRuleCreateDto> createValidator,
        IValidator<PointsRuleUpdateDto> updateValidator)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>
    /// 获取积分规则分页列表
    /// </summary>
    public async Task<ApiResponseDto<PagedResponseDto<PointsRuleDto>>> GetPagedListAsync(PointsRuleQueryDto query)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<PagedResponseDto<PointsRuleDto>>.Fail("无法确定当前租户", 401);

        var tenantId = _currentUser.TenantId.Value;
        var queryable = _dbContext.PointsRules
            .Where(p => !p.IsDeleted && p.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(query.Name))
            queryable = queryable.Where(p => p.Name.Contains(query.Name));
        if (query.Status.HasValue)
            queryable = queryable.Where(p => p.Status == query.Status.Value);

        var total = await queryable.CountAsync();
        var items = await queryable
            .OrderByDescending(p => p.CreatedTime)
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var result = new PagedResponseDto<PointsRuleDto>
        {
            List = items.Adapt<List<PointsRuleDto>>(),
            Total = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
        return ApiResponseDto<PagedResponseDto<PointsRuleDto>>.Ok(result);
    }

    /// <summary>
    /// 根据ID获取积分规则详情
    /// </summary>
    public async Task<ApiResponseDto<PointsRuleDto?>> GetByIdAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<PointsRuleDto?>.Fail("无法确定当前租户", 401);

        var entity = await _dbContext.PointsRules
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted && p.TenantId == _currentUser.TenantId.Value);
        if (entity == null)
            return ApiResponseDto<PointsRuleDto?>.Fail("积分规则不存在", 404);
        return ApiResponseDto<PointsRuleDto?>.Ok(entity.Adapt<PointsRuleDto>());
    }

    /// <summary>
    /// 创建积分规则
    /// </summary>
    public async Task<ApiResponseDto<PointsRuleDto>> CreateAsync(PointsRuleCreateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<PointsRuleDto>.Fail("无法确定当前租户", 401);

        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<PointsRuleDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var entity = dto.Adapt<PointsRuleEntity>();
        entity.TenantId = tenantId;
        entity.TenantCode = _currentUser.TenantCode ?? string.Empty;
        entity.CreatedTime = DateTime.Now;

        _dbContext.PointsRules.Add(entity);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto<PointsRuleDto>.Ok(entity.Adapt<PointsRuleDto>(), "创建成功");
    }

    /// <summary>
    /// 更新积分规则
    /// </summary>
    public async Task<ApiResponseDto<PointsRuleDto>> UpdateAsync(PointsRuleUpdateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<PointsRuleDto>.Fail("无法确定当前租户", 401);

        var validation = await _updateValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<PointsRuleDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var entity = await _dbContext.PointsRules
            .FirstOrDefaultAsync(p => p.Id == dto.Id && !p.IsDeleted && p.TenantId == tenantId);
        if (entity == null)
            return ApiResponseDto<PointsRuleDto>.Fail("积分规则不存在", 404);

        entity.Name = dto.Name;
        entity.PointsRate = dto.PointsRate;
        entity.DeductRate = dto.DeductRate;
        entity.MaxDeductAmount = dto.MaxDeductAmount;
        entity.PointsValidityDays = dto.PointsValidityDays;
        entity.BirthdayDouble = dto.BirthdayDouble;
        entity.MinPointsThreshold = dto.MinPointsThreshold;
        entity.Status = dto.Status;
        entity.Remark = dto.Remark;
        entity.UpdatedTime = DateTime.Now;

        await _dbContext.SaveChangesAsync();
        return ApiResponseDto<PointsRuleDto>.Ok(entity.Adapt<PointsRuleDto>(), "更新成功");
    }

    /// <summary>
    /// 删除积分规则（软删除）
    /// </summary>
    public async Task<ApiResponseDto> DeleteAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("无法确定当前租户", 401);

        var entity = await _dbContext.PointsRules
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted && p.TenantId == _currentUser.TenantId.Value);
        if (entity == null)
            return ApiResponseDto.Fail("积分规则不存在", 404);

        entity.IsDeleted = true;
        entity.UpdatedTime = DateTime.Now;
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, "删除成功");
    }

    /// <summary>
    /// 批量删除积分规则（软删除）
    /// </summary>
    public async Task<ApiResponseDto> BatchDeleteAsync(List<long> ids)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("无法确定当前租户", 401);
        if (ids == null || !ids.Any())
            return ApiResponseDto.Fail("请选择要删除的数据", 400);

        var entities = await _dbContext.PointsRules
            .Where(p => ids.Contains(p.Id) && !p.IsDeleted && p.TenantId == _currentUser.TenantId.Value)
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
