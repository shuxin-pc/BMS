using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Statistics;
using Bms.Store.Application.Services;

namespace Bms.Store.Api.Controllers;

/// <summary>
/// 效期销售统计控制器（B5.5）
/// 提供按商品 + 效期区间聚合的销售/消耗统计报表
/// </summary>
[ApiController]
[Route("api/store/[controller]")]
[Authorize]
public class ProductExpirySalesStatsController : ControllerBase
{
    private readonly IProductExpirySalesStatAppService _appService;

    public ProductExpirySalesStatsController(IProductExpirySalesStatAppService appService)
    {
        _appService = appService;
    }

    /// <summary>
    /// 获取效期销售统计报表
    /// 支持按时间范围、商品类型、分类、效期区间、关键词筛选
    /// </summary>
    [HttpGet("report")]
    public async Task<ApiResponseDto<PagedResponseDto<ProductExpirySalesStatDto>>> GetReport([FromQuery] ProductExpirySalesQueryDto query)
        => await _appService.GetReportAsync(query);
}
