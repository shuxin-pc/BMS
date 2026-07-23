using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.DailySettlements;

namespace Bms.Store.Application.Services;

/// <summary>
/// 日结管理应用服务接口
/// 提供日结汇总、确认、反日结、重算与防漏单校验能力
/// </summary>
public interface IDailySettlementAppService
{
    /// <summary>
    /// 获取日结记录分页列表
    /// </summary>
    Task<ApiResponseDto<PagedResponseDto<DailySettlementDto>>> GetPagedListAsync(DailySettlementQueryDto query);

    /// <summary>
    /// 根据ID获取日结记录详情
    /// </summary>
    Task<ApiResponseDto<DailySettlementDto?>> GetByIdAsync(long id);

    /// <summary>
    /// 获取今日经营汇总（实时聚合，不落库）
    /// </summary>
    Task<ApiResponseDto<TodaySummaryDto>> GetTodaySummaryAsync();

    /// <summary>
    /// 手动汇总日结（创建待确认记录）
    /// </summary>
    Task<ApiResponseDto<DailySettlementDto>> SummarizeAsync(ManualSummarizeRequestDto request);

    /// <summary>
    /// 确认日结（待确认 -> 已确认）
    /// </summary>
    Task<ApiResponseDto<DailySettlementDto>> ConfirmAsync(long id);

    /// <summary>
    /// 反日结（已确认 -> 待确认）
    /// </summary>
    Task<ApiResponseDto<DailySettlementDto>> ReverseAsync(long id, ReverseRequestDto request);

    /// <summary>
    /// 重新汇总（仅待确认状态可重算）
    /// </summary>
    Task<ApiResponseDto<DailySettlementDto>> RecalculateAsync(long id);

    /// <summary>
    /// 确认前防漏单校验
    /// </summary>
    Task<ApiResponseDto<SettlementValidationResultDto>> ValidateBeforeConfirmAsync(long id);

    /// <summary>
    /// 批量补日结（P-DS-07）
    /// 传入起止日期范围，循环为每个缺失日期创建日结记录
    /// </summary>
    Task<ApiResponseDto<BatchSummarizeResultDto>> BatchSummarizeAsync(BatchSummarizeRequestDto request);

    /// <summary>
    /// 历史漏日结回补（P-DS-09）
    /// 管理员手动触发，跨度 <= 90 天，endDate 必须早于今天
    /// </summary>
    Task<ApiResponseDto<BackfillResultDto>> BackfillAsync(DateTime startDate, DateTime endDate);

    /// <summary>
    /// 聚合指定门店指定日期的营业数据
    /// 无订单日返回全 0 数据。供跨服务调用复用（如跨日退款触发反日结时重新汇总原下单日数据）
    /// </summary>
    Task<SettlementSummaryData> SummarizeCoreAsync(long tenantId, long storeId, DateTime date, CancellationToken cancellationToken = default);
}
