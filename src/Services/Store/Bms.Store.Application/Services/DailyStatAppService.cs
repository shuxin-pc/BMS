using Mapster;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Bms.BuildingBlocks.Abstractions.Security;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Statistics;
using DailyStatEntity = Bms.Store.Domain.Entities.DailyStat;
using Bms.Store.Infrastructure;

namespace Bms.Store.Application.Services;

/// <summary>
/// 日统计应用服务实现
/// </summary>
public class DailyStatAppService : IDailyStatAppService
{
    private readonly StoreDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<DailyStatCreateDto> _createValidator;
    private readonly IValidator<DailyStatUpdateDto> _updateValidator;

    public DailyStatAppService(
        StoreDbContext dbContext,
        ICurrentUser currentUser,
        IValidator<DailyStatCreateDto> createValidator,
        IValidator<DailyStatUpdateDto> updateValidator)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>
    /// 获取日统计分页列表
    /// </summary>
    public async Task<ApiResponseDto<PagedResponseDto<DailyStatDto>>> GetPagedListAsync(DailyStatQueryDto query)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<PagedResponseDto<DailyStatDto>>.Fail("无法确定当前租户", 401);

        var tenantId = _currentUser.TenantId.Value;
        var queryable = _dbContext.DailyStats
            .Where(s => s.TenantId == tenantId);

        if (query.StartDate.HasValue)
            queryable = queryable.Where(s => s.StatDate >= query.StartDate.Value);
        if (query.EndDate.HasValue)
            queryable = queryable.Where(s => s.StatDate <= query.EndDate.Value);

        var total = await queryable.CountAsync();
        var items = await queryable
            .OrderByDescending(s => s.StatDate)
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var result = new PagedResponseDto<DailyStatDto>
        {
            List = items.Adapt<List<DailyStatDto>>(),
            Total = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
        return ApiResponseDto<PagedResponseDto<DailyStatDto>>.Ok(result);
    }

    /// <summary>
    /// 根据ID获取日统计详情
    /// </summary>
    public async Task<ApiResponseDto<DailyStatDto?>> GetByIdAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<DailyStatDto?>.Fail("无法确定当前租户", 401);

        var entity = await _dbContext.DailyStats
            .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == _currentUser.TenantId.Value);
        if (entity == null)
            return ApiResponseDto<DailyStatDto?>.Fail("日统计不存在", 404);
        return ApiResponseDto<DailyStatDto?>.Ok(entity.Adapt<DailyStatDto>());
    }

    /// <summary>
    /// 创建日统计
    /// </summary>
    public async Task<ApiResponseDto<DailyStatDto>> CreateAsync(DailyStatCreateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<DailyStatDto>.Fail("无法确定当前租户", 401);

        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<DailyStatDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var entity = dto.Adapt<DailyStatEntity>();
        entity.TenantId = tenantId;
        entity.TenantCode = _currentUser.TenantCode ?? string.Empty;
        entity.CreatedTime = DateTime.Now;

        _dbContext.DailyStats.Add(entity);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto<DailyStatDto>.Ok(entity.Adapt<DailyStatDto>(), "创建成功");
    }

    /// <summary>
    /// 更新日统计
    /// </summary>
    public async Task<ApiResponseDto<DailyStatDto>> UpdateAsync(DailyStatUpdateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<DailyStatDto>.Fail("无法确定当前租户", 401);

        var validation = await _updateValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<DailyStatDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var entity = await _dbContext.DailyStats
            .FirstOrDefaultAsync(s => s.Id == dto.Id && s.TenantId == tenantId);
        if (entity == null)
            return ApiResponseDto<DailyStatDto>.Fail("日统计不存在", 404);

        entity.StatDate = dto.StatDate;
        entity.Revenue = dto.Revenue;
        entity.Cost = dto.Cost;
        entity.GrossProfit = dto.GrossProfit;
        entity.OrderCount = dto.OrderCount;
        entity.RefundAmount = dto.RefundAmount;
        entity.StoredValueRecharge = dto.StoredValueRecharge;
        entity.StoredValueConsume = dto.StoredValueConsume;
        entity.ConsumeCustomerCount = dto.ConsumeCustomerCount;
        entity.NewCustomerCount = dto.NewCustomerCount;
        entity.AppointmentCount = dto.AppointmentCount;
        entity.InventoryAlertCount = dto.InventoryAlertCount;
        entity.UpdatedTime = DateTime.Now;

        await _dbContext.SaveChangesAsync();
        return ApiResponseDto<DailyStatDto>.Ok(entity.Adapt<DailyStatDto>(), "更新成功");
    }

    /// <summary>
    /// 删除日统计（物理删除）
    /// </summary>
    public async Task<ApiResponseDto> DeleteAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("无法确定当前租户", 401);

        var entity = await _dbContext.DailyStats
            .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == _currentUser.TenantId.Value);
        if (entity == null)
            return ApiResponseDto.Fail("日统计不存在", 404);

        _dbContext.DailyStats.Remove(entity);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, "删除成功");
    }

    /// <summary>
    /// 批量删除日统计（物理删除）
    /// </summary>
    public async Task<ApiResponseDto> BatchDeleteAsync(List<long> ids)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("无法确定当前租户", 401);
        if (ids == null || !ids.Any())
            return ApiResponseDto.Fail("请选择要删除的数据", 400);

        var entities = await _dbContext.DailyStats
            .Where(s => ids.Contains(s.Id) && s.TenantId == _currentUser.TenantId.Value)
            .ToListAsync();

        _dbContext.DailyStats.RemoveRange(entities);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, $"成功删除 {entities.Count} 条数据");
    }
}
