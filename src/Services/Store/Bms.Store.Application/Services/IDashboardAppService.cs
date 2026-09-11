using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Dashboard;
using Bms.Store.Application.Dtos.Statistics;

namespace Bms.Store.Application.Services;

/// <summary>
/// 首页看板聚合查询服务
/// </summary>
public interface IDashboardAppService
{
    /// <summary>
    /// 获取今日核心指标（实时聚合）
    /// </summary>
    Task<ApiResponseDto<DashboardSummaryDto>> GetDashboardSummaryAsync();

    /// <summary>
    /// 获取月度营收趋势（DailyStat历史 + 今日实时追加）
    /// </summary>
    Task<ApiResponseDto<List<DailyStatDto>>> GetMonthlyTrendAsync(MonthlyTrendQueryDto query);

    /// <summary>
    /// 获取热门商品 TOP N（查询 ProductSalesStat 表）
    /// </summary>
    Task<ApiResponseDto<List<ProductSalesStatDto>>> GetTopProductsAsync(TopProductsQueryDto query);

    /// <summary>
    /// 获取营收构成（按 ProductType 分组）
    /// </summary>
    Task<ApiResponseDto<List<RevenueCompositionItemDto>>> GetRevenueCompositionAsync(MonthlyTrendQueryDto query);

    /// <summary>
    /// 获取首页预警提醒（聚合库存预警/批次临期/项目卡到期/客户生日，按紧急度排序取前 10 条）
    /// </summary>
    Task<ApiResponseDto<List<DashboardAlertDto>>> GetDashboardAlertsAsync();
}
