using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Statistics;
using Bms.Store.Application.Services;

namespace Bms.Store.Api.Controllers;

/// <summary>
/// 日统计管理控制器
/// </summary>
[ApiController]
[Route("api/store/[controller]")]
[Authorize]
public class DailyStatsController : ControllerBase
{
    private readonly IDailyStatAppService _appService;

    public DailyStatsController(IDailyStatAppService appService)
    {
        _appService = appService;
    }

    /// <summary>
    /// 获取日统计分页列表
    /// </summary>
    [HttpGet]
    public async Task<ApiResponseDto<PagedResponseDto<DailyStatDto>>> GetList([FromQuery] DailyStatQueryDto query)
        => await _appService.GetPagedListAsync(query);

    /// <summary>
    /// 获取日统计详情
    /// </summary>
    [HttpGet("{id:long}")]
    public async Task<ApiResponseDto<DailyStatDto?>> GetById(long id)
        => await _appService.GetByIdAsync(id);

    /// <summary>
    /// 创建日统计
    /// </summary>
    [HttpPost]
    public async Task<ApiResponseDto<DailyStatDto>> Create([FromBody] DailyStatCreateDto dto)
        => await _appService.CreateAsync(dto);

    /// <summary>
    /// 更新日统计
    /// </summary>
    [HttpPut("{id:long}")]
    public async Task<ApiResponseDto<DailyStatDto>> Update(long id, [FromBody] DailyStatUpdateDto dto)
    {
        dto.Id = id;
        return await _appService.UpdateAsync(dto);
    }

    /// <summary>
    /// 删除日统计
    /// </summary>
    [HttpDelete("{id:long}")]
    public async Task<ApiResponseDto> Delete(long id)
        => await _appService.DeleteAsync(id);

    /// <summary>
    /// 批量删除日统计
    /// </summary>
    [HttpPost("batch")]
    public async Task<ApiResponseDto> BatchDelete([FromBody] BatchDeleteRequest request)
        => await _appService.BatchDeleteAsync(request.Ids);
}
