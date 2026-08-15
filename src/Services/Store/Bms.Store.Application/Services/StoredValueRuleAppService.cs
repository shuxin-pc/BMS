using Mapster;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Bms.BuildingBlocks.Abstractions.Security;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.StoredValues;
using Bms.Store.Domain.Entities;
using Bms.Store.Infrastructure;

namespace Bms.Store.Application.Services;

/// <summary>
/// 储值规则应用服务实现
/// </summary>
public class StoredValueRuleAppService : IStoredValueRuleAppService
{
    private readonly StoreDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IStoredValueGiftRuleService _giftRuleService;
    private readonly IValidator<StoredValueRuleCreateDto> _createValidator;
    private readonly IValidator<StoredValueRuleUpdateDto> _updateValidator;

    public StoredValueRuleAppService(
        StoreDbContext dbContext,
        ICurrentUser currentUser,
        IStoredValueGiftRuleService giftRuleService,
        IValidator<StoredValueRuleCreateDto> createValidator,
        IValidator<StoredValueRuleUpdateDto> updateValidator)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _giftRuleService = giftRuleService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>
    /// 获取储值规则分页列表
    /// </summary>
    public async Task<ApiResponseDto<PagedResponseDto<StoredValueRuleDto>>> GetPagedListAsync(StoredValueRuleQueryDto query)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<PagedResponseDto<StoredValueRuleDto>>.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;
        var queryable = _dbContext.StoredValueRules
            .Where(r => !r.IsDeleted && r.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(query.Name))
            queryable = queryable.Where(r => r.Name.Contains(query.Name));
        if (query.IsEnabled.HasValue)
            queryable = queryable.Where(r => r.IsEnabled == query.IsEnabled.Value);

        var total = await queryable.CountAsync();
        var items = await queryable
            .OrderByDescending(r => r.CreatedTime)
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var result = new PagedResponseDto<StoredValueRuleDto>
        {
            List = items.Select(ToDto).ToList(),
            Total = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
        return ApiResponseDto<PagedResponseDto<StoredValueRuleDto>>.Ok(result);
    }

    /// <summary>
    /// 根据ID获取储值规则详情
    /// </summary>
    public async Task<ApiResponseDto<StoredValueRuleDto?>> GetByIdAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<StoredValueRuleDto?>.Fail("登录状态异常，请重新登录", 401);

        var entity = await _dbContext.StoredValueRules
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted && r.TenantId == _currentUser.TenantId.Value);
        if (entity == null)
            return ApiResponseDto<StoredValueRuleDto?>.Fail("储值规则不存在", 404);
        return ApiResponseDto<StoredValueRuleDto?>.Ok(ToDto(entity));
    }

    /// <summary>
    /// 创建储值规则
    /// </summary>
    public async Task<ApiResponseDto<StoredValueRuleDto>> CreateAsync(StoredValueRuleCreateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<StoredValueRuleDto>.Fail("登录状态异常，请重新登录", 401);

        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<StoredValueRuleDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        // 同额档位并存会让"整倍叠加"计算取到不确定的一条，因此充值金额在租户内必须唯一
        var amountExists = await _dbContext.StoredValueRules
            .AnyAsync(r => r.Amount == dto.Amount && r.TenantId == tenantId && !r.IsDeleted);
        if (amountExists)
            return ApiResponseDto<StoredValueRuleDto>.Fail($"充值金额 {dto.Amount:F2} 已存在对应规则", 400);

        var entity = dto.Adapt<StoredValueRule>();
        entity.TenantId = tenantId;
        entity.TenantCode = _currentUser.TenantCode ?? string.Empty;
        entity.CreatedTime = DateTime.Now;

        _dbContext.StoredValueRules.Add(entity);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto<StoredValueRuleDto>.Ok(ToDto(entity), "创建成功");
    }

    /// <summary>
    /// 更新储值规则
    /// </summary>
    public async Task<ApiResponseDto<StoredValueRuleDto>> UpdateAsync(StoredValueRuleUpdateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<StoredValueRuleDto>.Fail("登录状态异常，请重新登录", 401);

        var validation = await _updateValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<StoredValueRuleDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var entity = await _dbContext.StoredValueRules
            .FirstOrDefaultAsync(r => r.Id == dto.Id && !r.IsDeleted && r.TenantId == tenantId);
        if (entity == null)
            return ApiResponseDto<StoredValueRuleDto>.Fail("储值规则不存在", 404);

        if (entity.Amount != dto.Amount)
        {
            var amountExists = await _dbContext.StoredValueRules
                .AnyAsync(r => r.Amount == dto.Amount && r.TenantId == tenantId && !r.IsDeleted && r.Id != dto.Id);
            if (amountExists)
                return ApiResponseDto<StoredValueRuleDto>.Fail($"充值金额 {dto.Amount:F2} 已存在对应规则", 400);
        }

        entity.Name = dto.Name;
        entity.Amount = dto.Amount;
        entity.GiftAmount = dto.GiftAmount;
        entity.StartDate = dto.StartDate;
        entity.EndDate = dto.EndDate;
        entity.IsEnabled = dto.IsEnabled;
        entity.Sort = dto.Sort;
        entity.Remark = dto.Remark;
        entity.UpdatedTime = DateTime.Now;

        await _dbContext.SaveChangesAsync();
        return ApiResponseDto<StoredValueRuleDto>.Ok(ToDto(entity), "更新成功");
    }

    /// <summary>
    /// 删除储值规则（软删除）
    /// </summary>
    public async Task<ApiResponseDto> DeleteAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("登录状态异常，请重新登录", 401);

        var entity = await _dbContext.StoredValueRules
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted && r.TenantId == _currentUser.TenantId.Value);
        if (entity == null)
            return ApiResponseDto.Fail("储值规则不存在", 404);

        entity.IsDeleted = true;
        entity.UpdatedTime = DateTime.Now;
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, "删除成功");
    }

    /// <summary>
    /// 批量删除储值规则（软删除）
    /// </summary>
    public async Task<ApiResponseDto> BatchDeleteAsync(List<long> ids)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("登录状态异常，请重新登录", 401);
        if (ids == null || !ids.Any())
            return ApiResponseDto.Fail("请选择要删除的数据", 400);

        var entities = await _dbContext.StoredValueRules
            .Where(r => ids.Contains(r.Id) && !r.IsDeleted && r.TenantId == _currentUser.TenantId.Value)
            .ToListAsync();

        foreach (var entity in entities)
        {
            entity.IsDeleted = true;
            entity.UpdatedTime = DateTime.Now;
        }
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, $"成功删除 {entities.Count} 条数据");
    }

    /// <summary>
    /// 试算充值赠送金额（与实际充值使用同一计算口径）
    /// </summary>
    public async Task<ApiResponseDto<StoredValueGiftPreviewDto>> PreviewGiftAmountAsync(decimal amount)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<StoredValueGiftPreviewDto>.Fail("登录状态异常，请重新登录", 401);

        if (amount <= 0)
            return ApiResponseDto<StoredValueGiftPreviewDto>.Fail("充值金额必须大于0", 400);

        var giftAmount = await _giftRuleService.CalculateGiftAmountAsync(_currentUser.TenantId.Value, amount, DateTime.Now);
        return ApiResponseDto<StoredValueGiftPreviewDto>.Ok(new StoredValueGiftPreviewDto
        {
            Amount = amount,
            GiftAmount = giftAmount
        });
    }

    /// <summary>
    /// 实体转 DTO（手动映射时间字段）
    /// </summary>
    private static StoredValueRuleDto ToDto(StoredValueRule entity)
    {
        var dto = entity.Adapt<StoredValueRuleDto>();
        dto.CreatedAt = entity.CreatedTime;
        dto.UpdatedAt = entity.UpdatedTime;
        return dto;
    }
}
