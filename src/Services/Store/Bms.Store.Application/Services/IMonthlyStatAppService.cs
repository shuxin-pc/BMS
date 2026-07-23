using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Statistics;

namespace Bms.Store.Application.Services;

/// <summary>
/// 月统计应用服务接口
/// </summary>
public interface IMonthlyStatAppService
{
    Task<ApiResponseDto<PagedResponseDto<MonthlyStatDto>>> GetPagedListAsync(MonthlyStatQueryDto query);
    Task<ApiResponseDto<MonthlyStatDto?>> GetByIdAsync(long id);
    Task<ApiResponseDto<MonthlyStatDto>> CreateAsync(MonthlyStatCreateDto dto);
    Task<ApiResponseDto<MonthlyStatDto>> UpdateAsync(MonthlyStatUpdateDto dto);
    Task<ApiResponseDto> DeleteAsync(long id);
    Task<ApiResponseDto> BatchDeleteAsync(List<long> ids);

    /// <summary>
    /// 按月份聚合 DailyStat 生成或更新 MonthlyStat（P-STAT-01）
    /// 聚合维度：Revenue/Cost/GrossProfit/OrderCount/RefundAmount/StoredValueRecharge/
    /// StoredValueConsume/TreatmentCardVerifyAmount/ConsumeCustomerCount/NewCustomerCount
    /// </summary>
    /// <param name="year">年份</param>
    /// <param name="month">月份（1-12）</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task<ApiResponseDto<MonthlyStatDto>> AggregateFromDailyAsync(int year, int month, CancellationToken cancellationToken = default);

    /// <summary>
    /// 按月份聚合 DailyStat 生成或更新 MonthlyStat（供 BackgroundService 跨门店批量调用）
    /// 不依赖 HttpContext，需显式传入租户与门店
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <param name="storeId">门店ID</param>
    /// <param name="tenantCode">租户编码</param>
    /// <param name="year">年份</param>
    /// <param name="month">月份（1-12）</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task<MonthlyStatDto?> AggregateFromDailyInternalAsync(
        long tenantId, long storeId, string tenantCode,
        int year, int month, CancellationToken cancellationToken = default);
}
