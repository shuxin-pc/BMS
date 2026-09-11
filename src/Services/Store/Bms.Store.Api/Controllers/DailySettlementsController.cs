using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.DailySettlements;
using Bms.Store.Application.Services;

namespace Bms.Store.Api.Controllers;

/// <summary>
/// 日结管理控制器
/// 手动汇总 -> 待确认 -> 确认 -> 已确认；支持反日结与重算
/// </summary>
[ApiController]
[Route("api/store/daily-settlements")]
[Authorize]
public class DailySettlementsController : ControllerBase
{
    private readonly IDailySettlementAppService _appService;

    public DailySettlementsController(IDailySettlementAppService appService)
    {
        _appService = appService;
    }

    /// <summary>
    /// 获取日结记录分页列表
    /// </summary>
    [HttpGet]
    public async Task<ApiResponseDto<PagedResponseDto<DailySettlementDto>>> GetList([FromQuery] DailySettlementQueryDto query)
        => await _appService.GetPagedListAsync(query);

    /// <summary>
    /// 获取日结记录详情
    /// </summary>
    [HttpGet("{id:long}")]
    public async Task<ApiResponseDto<DailySettlementDto?>> GetById(long id)
        => await _appService.GetByIdAsync(id);

    /// <summary>
    /// 获取今日经营汇总（实时聚合）
    /// </summary>
    [HttpGet("today-summary")]
    public async Task<ApiResponseDto<TodaySummaryDto>> GetTodaySummary()
        => await _appService.GetTodaySummaryAsync();

    /// <summary>
    /// 手动汇总日结（创建待确认记录）
    /// </summary>
    [HttpPost("summarize")]
    public async Task<ApiResponseDto<DailySettlementDto>> Summarize([FromBody] ManualSummarizeRequestDto request)
        => await _appService.SummarizeAsync(request);

    /// <summary>
    /// 确认前防漏单校验
    /// </summary>
    [HttpGet("{id:long}/validate")]
    public async Task<ApiResponseDto<SettlementValidationResultDto>> Validate(long id)
        => await _appService.ValidateBeforeConfirmAsync(id);

    /// <summary>
    /// 确认日结（待确认 -> 已确认），确认时可修改备注
    /// </summary>
    [HttpPost("{id:long}/confirm")]
    public async Task<ApiResponseDto<DailySettlementDto>> Confirm(long id, [FromBody] ConfirmRequestDto? request)
        => await _appService.ConfirmAsync(id, request?.Remark);

    /// <summary>
    /// 反日结（已确认 -> 待确认）
    /// </summary>
    [HttpPost("{id:long}/reverse")]
    public async Task<ApiResponseDto<DailySettlementDto>> Reverse(long id, [FromBody] ReverseRequestDto? request)
        => await _appService.ReverseAsync(id, request ?? new ReverseRequestDto());

    /// <summary>
    /// 重新汇总（仅待确认状态可重算）
    /// </summary>
    [HttpPost("{id:long}/recalculate")]
    public async Task<ApiResponseDto<DailySettlementDto>> Recalculate(long id)
        => await _appService.RecalculateAsync(id);

    /// <summary>
    /// 批量补日结（P-DS-07）
    /// 选择起止日期批量创建日结记录，支持 AutoConfirm 自动确认
    /// </summary>
    [HttpPost("batch-summarize")]
    public async Task<ApiResponseDto<BatchSummarizeResultDto>> BatchSummarize([FromBody] BatchSummarizeRequestDto request)
        => await _appService.BatchSummarizeAsync(request);

    /// <summary>
    /// 历史漏日结回补（P-DS-09）
    /// 管理员手动触发，跨度 <= 90 天，endDate 必须早于今天
    /// </summary>
    [HttpPost("backfill")]
    public async Task<ApiResponseDto<BackfillResultDto>> Backfill([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        => await _appService.BackfillAsync(startDate, endDate);
}
