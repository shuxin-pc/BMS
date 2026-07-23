using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Statistics;
using Bms.Store.Application.Services;

namespace Bms.Store.Api.Controllers;

/// <summary>
/// 月统计管理控制器
/// </summary>
[ApiController]
[Route("api/store/[controller]")]
[Authorize]
public class MonthlyStatsController : ControllerBase
{
    private readonly IMonthlyStatAppService _appService;

    public MonthlyStatsController(IMonthlyStatAppService appService)
    {
        _appService = appService;
    }

    /// <summary>
    /// 获取月统计分页列表
    /// </summary>
    [HttpGet]
    public async Task<ApiResponseDto<PagedResponseDto<MonthlyStatDto>>> GetList([FromQuery] MonthlyStatQueryDto query)
        => await _appService.GetPagedListAsync(query);

    /// <summary>
    /// 获取月统计详情
    /// </summary>
    [HttpGet("{id:long}")]
    public async Task<ApiResponseDto<MonthlyStatDto?>> GetById(long id)
        => await _appService.GetByIdAsync(id);

    /// <summary>
    /// 创建月统计
    /// </summary>
    [HttpPost]
    public async Task<ApiResponseDto<MonthlyStatDto>> Create([FromBody] MonthlyStatCreateDto dto)
        => await _appService.CreateAsync(dto);

    /// <summary>
    /// 更新月统计
    /// </summary>
    [HttpPut("{id:long}")]
    public async Task<ApiResponseDto<MonthlyStatDto>> Update(long id, [FromBody] MonthlyStatUpdateDto dto)
    {
        dto.Id = id;
        return await _appService.UpdateAsync(dto);
    }

    /// <summary>
    /// 删除月统计
    /// </summary>
    [HttpDelete("{id:long}")]
    public async Task<ApiResponseDto> Delete(long id)
        => await _appService.DeleteAsync(id);

    /// <summary>
    /// 批量删除月统计
    /// </summary>
    [HttpPost("batch")]
    public async Task<ApiResponseDto> BatchDelete([FromBody] BatchDeleteRequest request)
        => await _appService.BatchDeleteAsync(request.Ids);

    /// <summary>
    /// 手动触发月度统计聚合（按月份从 DailyStat 汇总生成 MonthlyStat）（P-STAT-01）
    /// </summary>
    /// <param name="year">年份</param>
    /// <param name="month">月份（1-12）</param>
    [HttpPost("aggregate")]
    public async Task<ApiResponseDto<MonthlyStatDto>> Aggregate([FromQuery] int year, [FromQuery] int month)
        => await _appService.AggregateFromDailyAsync(year, month);
}
