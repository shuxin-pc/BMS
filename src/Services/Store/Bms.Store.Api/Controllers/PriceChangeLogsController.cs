using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.PriceChangeLogs;
using Bms.Store.Application.Services;

namespace Bms.Store.Api.Controllers;

/// <summary>
/// 价格变更记录查询控制器（只读，价格变更由档案编辑/主档统一配置自动写入日志）
/// </summary>
[ApiController]
[Route("api/store/[controller]")]
[Authorize]
public class PriceChangeLogsController : ControllerBase
{
    private readonly IPriceChangeLogAppService _appService;

    public PriceChangeLogsController(IPriceChangeLogAppService appService)
    {
        _appService = appService;
    }

    /// <summary>
    /// 获取价格变更记录分页列表
    /// </summary>
    [HttpGet]
    public async Task<ApiResponseDto<PagedResponseDto<PriceChangeLogDto>>> GetList([FromQuery] PriceChangeLogQueryDto query)
        => await _appService.GetPagedListAsync(query);

    /// <summary>
    /// 获取价格变更记录详情
    /// </summary>
    [HttpGet("{id:long}")]
    public async Task<ApiResponseDto<PriceChangeLogDto?>> GetById(long id)
        => await _appService.GetByIdAsync(id);
}
