using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Dashboard;
using Bms.Store.Application.Dtos.Statistics;
using Bms.Store.Application.Services;

namespace Bms.Store.Api.Controllers;

/// <summary>
/// 首页看板聚合查询控制器
/// 提供今日核心指标、月度趋势、热门商品TOP5、营收构成查询
/// </summary>
[ApiController]
[Route("api/store/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardAppService _appService;

    public DashboardController(IDashboardAppService appService)
    {
        _appService = appService;
    }

    /// <summary>
    /// 获取今日核心指标（实时聚合）
    /// </summary>
    [HttpGet("summary")]
    public async Task<ApiResponseDto<DashboardSummaryDto>> GetSummary()
        => await _appService.GetDashboardSummaryAsync();

    /// <summary>
    /// 获取月度营收趋势
    /// </summary>
    [HttpGet("monthly-trend")]
    public async Task<ApiResponseDto<List<DailyStatDto>>> GetMonthlyTrend([FromQuery] MonthlyTrendQueryDto query)
        => await _appService.GetMonthlyTrendAsync(query);

    /// <summary>
    /// 获取热门商品 TOP N
    /// </summary>
    [HttpGet("top-products")]
    public async Task<ApiResponseDto<List<ProductSalesStatDto>>> GetTopProducts([FromQuery] TopProductsQueryDto query)
        => await _appService.GetTopProductsAsync(query);

    /// <summary>
    /// 获取营收构成（按商品类型分组）
    /// </summary>
    [HttpGet("revenue-composition")]
    public async Task<ApiResponseDto<List<RevenueCompositionItemDto>>> GetRevenueComposition([FromQuery] MonthlyTrendQueryDto query)
        => await _appService.GetRevenueCompositionAsync(query);
}
