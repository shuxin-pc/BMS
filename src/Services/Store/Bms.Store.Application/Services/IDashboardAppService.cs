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
    /// 获取热门商品 TOP N（仅零售+耗材，ProductType=1,3）（P-DS-05）
    /// </summary>
    Task<ApiResponseDto<List<ProductSalesStatDto>>> GetTopProductsBySalesAsync(TopProductsQueryDto query);

    /// <summary>
    /// 获取热门服务 TOP N（仅服务+疗程卡购买，ProductType=2,4）（P-DS-05）
    /// </summary>
    Task<ApiResponseDto<List<ProductSalesStatDto>>> GetTopServicesBySalesAsync(TopProductsQueryDto query);

    /// <summary>
    /// 获取营收构成（按 ProductType 分组）
    /// </summary>
    Task<ApiResponseDto<List<RevenueCompositionItemDto>>> GetRevenueCompositionAsync(MonthlyTrendQueryDto query);
}
