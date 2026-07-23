using Mapster;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Bms.BuildingBlocks.Abstractions.Security;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.TreatmentCards;
using Bms.Store.Domain.Entities;
using Bms.Store.Infrastructure;

namespace Bms.Store.Application.Services;

/// <summary>
/// 疗程卡配置应用服务实现
/// 主表 TreatmentCard + 子表 CourseCardItem 联动管理：
/// - 创建/更新时按项目原价比例分摊卡价（Price），计算并锁定折算单价（AllocatedUnitPrice）
/// - 核销时按锁定的折算单价计入营收
/// </summary>
public class TreatmentCardAppService : ITreatmentCardAppService
{
    private readonly StoreDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<TreatmentCardCreateDto> _createValidator;
    private readonly IValidator<TreatmentCardUpdateDto> _updateValidator;

    public TreatmentCardAppService(
        StoreDbContext dbContext,
        ICurrentUser currentUser,
        IValidator<TreatmentCardCreateDto> createValidator,
        IValidator<TreatmentCardUpdateDto> updateValidator)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>
    /// 获取疗程卡配置分页列表（含项目明细）
    /// </summary>
    public async Task<ApiResponseDto<PagedResponseDto<TreatmentCardDto>>> GetPagedListAsync(TreatmentCardQueryDto query)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<PagedResponseDto<TreatmentCardDto>>.Fail("无法确定当前租户", 401);

        var tenantId = _currentUser.TenantId.Value;
        var queryable = _dbContext.TreatmentCards
            .Where(t => !t.IsDeleted && t.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(query.Name))
            queryable = queryable.Where(t => t.Name.Contains(query.Name));
        if (!string.IsNullOrWhiteSpace(query.Code))
            queryable = queryable.Where(t => t.Code.Contains(query.Code));
        if (query.IsEnabled.HasValue)
            queryable = queryable.Where(t => t.IsEnabled == query.IsEnabled.Value);

        var total = await queryable.CountAsync();
        var items = await queryable
            .OrderByDescending(t => t.CreatedTime)
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var dtoList = items.Select(ToDto).ToList();
        await FillItemsAsync(dtoList, tenantId);

        var result = new PagedResponseDto<TreatmentCardDto>
        {
            List = dtoList,
            Total = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
        return ApiResponseDto<PagedResponseDto<TreatmentCardDto>>.Ok(result);
    }

    /// <summary>
    /// 根据ID获取疗程卡配置详情（含项目明细）
    /// </summary>
    public async Task<ApiResponseDto<TreatmentCardDto?>> GetByIdAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<TreatmentCardDto?>.Fail("无法确定当前租户", 401);

        var tenantId = _currentUser.TenantId.Value;
        var entity = await _dbContext.TreatmentCards
            .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted && t.TenantId == tenantId);
        if (entity == null)
            return ApiResponseDto<TreatmentCardDto?>.Fail("疗程卡配置不存在", 404);

        var dto = ToDto(entity);
        await FillItemsForCardAsync(dto, tenantId);
        return ApiResponseDto<TreatmentCardDto?>.Ok(dto);
    }

    /// <summary>
    /// 创建疗程卡配置（同时创建项目明细子表，计算折算单价）
    /// </summary>
    public async Task<ApiResponseDto<TreatmentCardDto>> CreateAsync(TreatmentCardCreateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<TreatmentCardDto>.Fail("无法确定当前租户", 401);

        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<TreatmentCardDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var codeExists = await _dbContext.TreatmentCards
            .AnyAsync(t => t.Code == dto.Code && t.TenantId == tenantId && !t.IsDeleted);
        if (codeExists)
            return ApiResponseDto<TreatmentCardDto>.Fail($"编码 {dto.Code} 已存在", 400);

        var entity = dto.Adapt<TreatmentCard>();
        entity.TenantId = tenantId;
        entity.TenantCode = _currentUser.TenantCode ?? string.Empty;
        entity.CreatedTime = DateTime.Now;

        _dbContext.TreatmentCards.Add(entity);
        await _dbContext.SaveChangesAsync();

        // 创建项目明细子表，计算折算单价
        await CreateItemsAsync(dto.Items, entity.Id, entity.Price, tenantId);

        var result = ToDto(entity);
        await FillItemsForCardAsync(result, tenantId);
        return ApiResponseDto<TreatmentCardDto>.Ok(result, "创建成功");
    }

    /// <summary>
    /// 更新疗程卡配置（同时更新项目明细子表）
    /// </summary>
    public async Task<ApiResponseDto<TreatmentCardDto>> UpdateAsync(TreatmentCardUpdateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<TreatmentCardDto>.Fail("无法确定当前租户", 401);

        var validation = await _updateValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<TreatmentCardDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var entity = await _dbContext.TreatmentCards
            .FirstOrDefaultAsync(t => t.Id == dto.Id && !t.IsDeleted && t.TenantId == tenantId);
        if (entity == null)
            return ApiResponseDto<TreatmentCardDto>.Fail("疗程卡配置不存在", 404);

        if (entity.Code != dto.Code)
        {
            var codeExists = await _dbContext.TreatmentCards
                .AnyAsync(t => t.Code == dto.Code && t.TenantId == tenantId && !t.IsDeleted && t.Id != dto.Id);
            if (codeExists)
                return ApiResponseDto<TreatmentCardDto>.Fail($"编码 {dto.Code} 已存在", 400);
        }

        entity.StoreId = dto.StoreId;
        entity.StoreCode = dto.StoreCode;
        entity.Name = dto.Name;
        entity.Code = dto.Code;
        entity.ServiceItems = dto.ServiceItems;
        entity.TotalTimes = dto.TotalTimes;
        entity.Price = dto.Price;
        entity.ValidityDays = dto.ValidityDays;
        entity.IsEnabled = dto.IsEnabled;
        entity.Remark = dto.Remark;
        entity.UpdatedTime = DateTime.Now;

        await _dbContext.SaveChangesAsync();

        // 更新项目明细：先软删除旧项目，再创建新项目
        await SoftDeleteItemsAsync(entity.Id, tenantId);
        await CreateItemsAsync(dto.Items, entity.Id, entity.Price, tenantId);

        var result = ToDto(entity);
        await FillItemsForCardAsync(result, tenantId);
        return ApiResponseDto<TreatmentCardDto>.Ok(result, "更新成功");
    }

    /// <summary>
    /// 删除疗程卡配置（软删除，同时软删除项目明细）
    /// </summary>
    public async Task<ApiResponseDto> DeleteAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("无法确定当前租户", 401);

        var tenantId = _currentUser.TenantId.Value;
        var entity = await _dbContext.TreatmentCards
            .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted && t.TenantId == tenantId);
        if (entity == null)
            return ApiResponseDto.Fail("疗程卡配置不存在", 404);

        entity.IsDeleted = true;
        entity.UpdatedTime = DateTime.Now;
        await SoftDeleteItemsAsync(entity.Id, tenantId);

        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, "删除成功");
    }

    /// <summary>
    /// 批量删除疗程卡配置（软删除，同时软删除项目明细）
    /// </summary>
    public async Task<ApiResponseDto> BatchDeleteAsync(List<long> ids)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("无法确定当前租户", 401);
        if (ids == null || !ids.Any())
            return ApiResponseDto.Fail("请选择要删除的数据", 400);

        var tenantId = _currentUser.TenantId.Value;
        var entities = await _dbContext.TreatmentCards
            .Where(t => ids.Contains(t.Id) && !t.IsDeleted && t.TenantId == tenantId)
            .ToListAsync();

        foreach (var entity in entities)
        {
            entity.IsDeleted = true;
            entity.UpdatedTime = DateTime.Now;
            await SoftDeleteItemsAsync(entity.Id, tenantId);
        }
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, $"成功删除 {entities.Count} 条数据");
    }

    // ========== 子表辅助方法 ==========

    /// <summary>
    /// 批量填充项目明细到 DTO 列表
    /// </summary>
    private async Task FillItemsAsync(List<TreatmentCardDto> dtos, long tenantId)
    {
        if (!dtos.Any()) return;

        var cardIds = dtos.Select(d => d.Id).ToList();
        var items = await _dbContext.CourseCardItems
            .Where(i => cardIds.Contains(i.CourseCardId) && !i.IsDeleted)
            .ToListAsync();

        var itemsByCard = items.GroupBy(i => i.CourseCardId).ToDictionary(g => g.Key, g => g.ToList());
        foreach (var dto in dtos)
        {
            if (itemsByCard.TryGetValue(dto.Id, out var cardItems))
            {
                dto.Items = cardItems.Select(i => new CourseCardItemDto
                {
                    Id = i.Id,
                    CourseCardId = i.CourseCardId,
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    OriginalPrice = i.OriginalPrice,
                    AllocatedUnitPrice = i.AllocatedUnitPrice,
                    AllocatedTotalPrice = i.AllocatedTotalPrice,
                    CreatedAt = i.CreatedTime,
                    UpdatedAt = i.UpdatedTime
                }).ToList();
            }
        }
    }

    /// <summary>
    /// 填充单个疗程卡的项目明细
    /// </summary>
    private async Task FillItemsForCardAsync(TreatmentCardDto dto, long tenantId)
    {
        var items = await _dbContext.CourseCardItems
            .Where(i => i.CourseCardId == dto.Id && !i.IsDeleted)
            .ToListAsync();

        dto.Items = items.Select(i => new CourseCardItemDto
        {
            Id = i.Id,
            CourseCardId = i.CourseCardId,
            ProductId = i.ProductId,
            Quantity = i.Quantity,
            OriginalPrice = i.OriginalPrice,
            AllocatedUnitPrice = i.AllocatedUnitPrice,
            AllocatedTotalPrice = i.AllocatedTotalPrice,
            CreatedAt = i.CreatedTime,
            UpdatedAt = i.UpdatedTime
        }).ToList();
    }

    /// <summary>
    /// 创建项目明细子表，按项目原价比例分摊卡价计算折算单价
    /// 折算逻辑：各项目分摊价值 = 卡价 × (该项目原价 × 次数 / Σ所有项目原价 × 次数)
    /// </summary>
    private async Task CreateItemsAsync(List<CourseCardItemCreateDto> items, long cardId, decimal cardPrice, long tenantId)
    {
        if (items == null || !items.Any()) return;

        var tenantCode = _currentUser.TenantCode ?? string.Empty;
        var totalOriginal = items.Sum(i => i.OriginalPrice * i.Quantity);

        foreach (var item in items)
        {
            // 按原价比例分摊卡价
            var ratio = totalOriginal > 0
                ? (item.OriginalPrice * item.Quantity) / totalOriginal
                : 0;
            var allocatedTotal = cardPrice * ratio;
            var allocatedUnit = item.Quantity > 0
                ? allocatedTotal / item.Quantity
                : 0;

            _dbContext.CourseCardItems.Add(new CourseCardItem
            {
                CourseCardId = cardId,
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                OriginalPrice = item.OriginalPrice,
                AllocatedUnitPrice = allocatedUnit,
                AllocatedTotalPrice = allocatedTotal,
                TenantId = tenantId,
                TenantCode = tenantCode,
                CreatedTime = DateTime.Now
            });
        }
        await _dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// 软删除项目明细子表
    /// </summary>
    private async Task SoftDeleteItemsAsync(long cardId, long tenantId)
    {
        var items = await _dbContext.CourseCardItems
            .Where(i => i.CourseCardId == cardId && !i.IsDeleted)
            .ToListAsync();
        foreach (var item in items)
        {
            item.IsDeleted = true;
            item.UpdatedTime = DateTime.Now;
        }
    }

    /// <summary>
    /// 实体转 DTO（手动映射时间字段）
    /// </summary>
    private static TreatmentCardDto ToDto(TreatmentCard entity)
    {
        var dto = entity.Adapt<TreatmentCardDto>();
        dto.CreatedAt = entity.CreatedTime;
        dto.UpdatedAt = entity.UpdatedTime;
        return dto;
    }
}
