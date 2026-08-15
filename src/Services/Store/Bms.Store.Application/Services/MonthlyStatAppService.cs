using Mapster;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Bms.BuildingBlocks.Abstractions.Security;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Statistics;
using Bms.Store.Domain.Entities;
using MonthlyStatEntity = Bms.Store.Domain.Entities.MonthlyStat;
using Bms.Store.Infrastructure;

namespace Bms.Store.Application.Services;

/// <summary>
/// 月统计应用服务实现
/// 月统计数据 = SUM(日统计数据)，通过 AggregateFromDailyAsync 从 DailyStat 聚合生成（P-STAT-01）
/// </summary>
public class MonthlyStatAppService : IMonthlyStatAppService
{
    private readonly StoreDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<MonthlyStatCreateDto> _createValidator;
    private readonly IValidator<MonthlyStatUpdateDto> _updateValidator;

    public MonthlyStatAppService(
        StoreDbContext dbContext,
        ICurrentUser currentUser,
        IValidator<MonthlyStatCreateDto> createValidator,
        IValidator<MonthlyStatUpdateDto> updateValidator)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>
    /// 获取月统计分页列表
    /// </summary>
    public async Task<ApiResponseDto<PagedResponseDto<MonthlyStatDto>>> GetPagedListAsync(MonthlyStatQueryDto query)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<PagedResponseDto<MonthlyStatDto>>.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;
        var queryable = _dbContext.MonthlyStats
            .Where(s => s.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(query.StatMonth))
            queryable = queryable.Where(s => s.StatMonth == query.StatMonth);

        var total = await queryable.CountAsync();
        var items = await queryable
            .OrderByDescending(s => s.StatMonth)
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var result = new PagedResponseDto<MonthlyStatDto>
        {
            List = items.Adapt<List<MonthlyStatDto>>(),
            Total = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
        return ApiResponseDto<PagedResponseDto<MonthlyStatDto>>.Ok(result);
    }

    /// <summary>
    /// 根据ID获取月统计详情
    /// </summary>
    public async Task<ApiResponseDto<MonthlyStatDto?>> GetByIdAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<MonthlyStatDto?>.Fail("登录状态异常，请重新登录", 401);

        var entity = await _dbContext.MonthlyStats
            .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == _currentUser.TenantId.Value);
        if (entity == null)
            return ApiResponseDto<MonthlyStatDto?>.Fail("月统计不存在", 404);
        return ApiResponseDto<MonthlyStatDto?>.Ok(entity.Adapt<MonthlyStatDto>());
    }

    /// <summary>
    /// 创建月统计
    /// </summary>
    public async Task<ApiResponseDto<MonthlyStatDto>> CreateAsync(MonthlyStatCreateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<MonthlyStatDto>.Fail("登录状态异常，请重新登录", 401);

        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<MonthlyStatDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var entity = dto.Adapt<MonthlyStatEntity>();
        entity.TenantId = tenantId;
        entity.TenantCode = _currentUser.TenantCode ?? string.Empty;
        entity.CreatedTime = DateTime.Now;

        _dbContext.MonthlyStats.Add(entity);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto<MonthlyStatDto>.Ok(entity.Adapt<MonthlyStatDto>(), "创建成功");
    }

    /// <summary>
    /// 更新月统计
    /// </summary>
    public async Task<ApiResponseDto<MonthlyStatDto>> UpdateAsync(MonthlyStatUpdateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<MonthlyStatDto>.Fail("登录状态异常，请重新登录", 401);

        var validation = await _updateValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<MonthlyStatDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var entity = await _dbContext.MonthlyStats
            .FirstOrDefaultAsync(s => s.Id == dto.Id && s.TenantId == tenantId);
        if (entity == null)
            return ApiResponseDto<MonthlyStatDto>.Fail("月统计不存在", 404);

        entity.StatMonth = dto.StatMonth;
        entity.Revenue = dto.Revenue;
        entity.Cost = dto.Cost;
        entity.GrossProfit = dto.GrossProfit;
        entity.OrderCount = dto.OrderCount;
        entity.RefundAmount = dto.RefundAmount;
        entity.StoredValueRecharge = dto.StoredValueRecharge;
        entity.StoredValueConsume = dto.StoredValueConsume;
        entity.UpdatedTime = DateTime.Now;

        await _dbContext.SaveChangesAsync();
        return ApiResponseDto<MonthlyStatDto>.Ok(entity.Adapt<MonthlyStatDto>(), "更新成功");
    }

    /// <summary>
    /// 删除月统计（物理删除）
    /// </summary>
    public async Task<ApiResponseDto> DeleteAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("登录状态异常，请重新登录", 401);

        var entity = await _dbContext.MonthlyStats
            .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == _currentUser.TenantId.Value);
        if (entity == null)
            return ApiResponseDto.Fail("月统计不存在", 404);

        _dbContext.MonthlyStats.Remove(entity);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, "删除成功");
    }

    /// <summary>
    /// 批量删除月统计（物理删除）
    /// </summary>
    public async Task<ApiResponseDto> BatchDeleteAsync(List<long> ids)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("登录状态异常，请重新登录", 401);
        if (ids == null || !ids.Any())
            return ApiResponseDto.Fail("请选择要删除的数据", 400);

        var entities = await _dbContext.MonthlyStats
            .Where(s => ids.Contains(s.Id) && s.TenantId == _currentUser.TenantId.Value)
            .ToListAsync();

        _dbContext.MonthlyStats.RemoveRange(entities);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, $"成功删除 {entities.Count} 条数据");
    }

    /// <summary>
    /// 按月份聚合 DailyStat 生成或更新 MonthlyStat（P-STAT-01）
    /// 供 API 端点手动触发，使用当前用户上下文解析租户与门店
    /// </summary>
    public async Task<ApiResponseDto<MonthlyStatDto>> AggregateFromDailyAsync(int year, int month, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<MonthlyStatDto>.Fail("登录状态异常，请重新登录", 401);
        if (!_currentUser.StoreId.HasValue)
            return ApiResponseDto<MonthlyStatDto>.Fail("请选择门店", 400);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId.Value;
        var tenantCode = _currentUser.TenantCode ?? string.Empty;

        var result = await AggregateFromDailyInternalAsync(tenantId, storeId, tenantCode, year, month, cancellationToken);
        if (result == null)
            return ApiResponseDto<MonthlyStatDto>.Fail("聚合失败", 500);

        return ApiResponseDto<MonthlyStatDto>.Ok(result, "月度统计聚合完成");
    }

    /// <summary>
    /// 按月份聚合 DailyStat 生成或更新 MonthlyStat（P-STAT-01）
    /// 供 BackgroundService 跨门店批量调用，不依赖 HttpContext
    /// 聚合维度：Revenue/Cost/GrossProfit/OrderCount/RefundAmount/StoredValueRecharge/
    /// StoredValueConsume/TreatmentCardVerifyAmount/ConsumeCustomerCount/NewCustomerCount
    /// </summary>
    public async Task<MonthlyStatDto?> AggregateFromDailyInternalAsync(
        long tenantId, long storeId, string tenantCode,
        int year, int month, CancellationToken cancellationToken = default)
    {
        if (month < 1 || month > 12)
            return null;

        var monthStart = new DateTime(year, month, 1);
        var monthEnd = monthStart.AddMonths(1);
        var statMonth = $"{year}-{month:D2}";

        // 拉取当月所有 DailyStat 记录
        var dailyStats = await _dbContext.DailyStats
            .Where(d => d.TenantId == tenantId && d.StoreId == storeId
                && d.StatDate >= monthStart && d.StatDate < monthEnd)
            .ToListAsync(cancellationToken);

        // 聚合各维度（与文档 P-STAT-01 修复方案一致）
        var revenue = dailyStats.Sum(d => d.Revenue);
        var cashRevenue = dailyStats.Sum(d => d.CashRevenue);
        var storedValueRevenue = dailyStats.Sum(d => d.StoredValueRevenue);
        var pointsDeductAmount = dailyStats.Sum(d => d.PointsDeductAmount);
        var cost = dailyStats.Sum(d => d.Cost);
        var grossProfit = dailyStats.Sum(d => d.GrossProfit);
        var orderCount = dailyStats.Sum(d => d.OrderCount);
        var refundAmount = dailyStats.Sum(d => d.RefundAmount);
        var storedValueRecharge = dailyStats.Sum(d => d.StoredValueRecharge);
        var storedValueConsume = dailyStats.Sum(d => d.StoredValueConsume);
        var treatmentCardVerifyAmount = dailyStats.Sum(d => d.TreatmentCardVerifyAmount);
        var consumeCustomerCount = dailyStats.Sum(d => d.ConsumeCustomerCount);
        var newCustomerCount = dailyStats.Sum(d => d.NewCustomerCount);

        // 查找门店编码
        var storeCode = await _dbContext.Stores
            .Where(s => s.TenantId == tenantId && s.Id == storeId)
            .Select(s => s.Code)
            .FirstOrDefaultAsync(cancellationToken);

        // upsert：存在则更新，不存在则插入
        var existing = await _dbContext.MonthlyStats
            .FirstOrDefaultAsync(m => m.TenantId == tenantId && m.StoreId == storeId && m.StatMonth == statMonth, cancellationToken);

        if (existing != null)
        {
            existing.Revenue = revenue;
            existing.CashRevenue = cashRevenue;
            existing.StoredValueRevenue = storedValueRevenue;
            existing.PointsDeductAmount = pointsDeductAmount;
            existing.Cost = cost;
            existing.GrossProfit = grossProfit;
            existing.OrderCount = orderCount;
            existing.RefundAmount = refundAmount;
            existing.StoredValueRecharge = storedValueRecharge;
            existing.StoredValueConsume = storedValueConsume;
            existing.TreatmentCardVerifyAmount = treatmentCardVerifyAmount;
            existing.ConsumeCustomerCount = consumeCustomerCount;
            existing.NewCustomerCount = newCustomerCount;
            existing.UpdatedTime = DateTime.Now;

            await _dbContext.SaveChangesAsync(cancellationToken);
            return existing.Adapt<MonthlyStatDto>();
        }

        var entity = new MonthlyStatEntity
        {
            StatMonth = statMonth,
            Revenue = revenue,
            CashRevenue = cashRevenue,
            StoredValueRevenue = storedValueRevenue,
            PointsDeductAmount = pointsDeductAmount,
            Cost = cost,
            GrossProfit = grossProfit,
            OrderCount = orderCount,
            RefundAmount = refundAmount,
            StoredValueRecharge = storedValueRecharge,
            StoredValueConsume = storedValueConsume,
            TreatmentCardVerifyAmount = treatmentCardVerifyAmount,
            ConsumeCustomerCount = consumeCustomerCount,
            NewCustomerCount = newCustomerCount,
            TenantId = tenantId,
            TenantCode = tenantCode,
            StoreId = storeId,
            StoreCode = storeCode ?? string.Empty,
            CreatedTime = DateTime.Now
        };

        _dbContext.MonthlyStats.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return entity.Adapt<MonthlyStatDto>();
    }
}
