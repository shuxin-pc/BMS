using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.PriceChangeLogs;
using Bms.Store.Application.Services;

namespace Bms.Store.Api.Controllers;

/// <summary>
/// 价格变更记录管理控制器
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

    /// <summary>
    /// 创建价格变更记录
    /// </summary>
    [HttpPost]
    public async Task<ApiResponseDto<PriceChangeLogDto>> Create([FromBody] PriceChangeLogCreateDto dto)
        => await _appService.CreateAsync(dto);

    /// <summary>
    /// 更新价格变更记录
    /// </summary>
    [HttpPut("{id:long}")]
    public async Task<ApiResponseDto<PriceChangeLogDto>> Update(long id, [FromBody] PriceChangeLogUpdateDto dto)
    {
        dto.Id = id;
        return await _appService.UpdateAsync(dto);
    }

    /// <summary>
    /// 删除价格变更记录
    /// </summary>
    [HttpDelete("{id:long}")]
    public async Task<ApiResponseDto> Delete(long id)
        => await _appService.DeleteAsync(id);

    /// <summary>
    /// 批量删除价格变更记录
    /// </summary>
    [HttpPost("batch")]
    public async Task<ApiResponseDto> BatchDelete([FromBody] BatchDeleteRequest request)
        => await _appService.BatchDeleteAsync(request.Ids);

    /// <summary>
    /// 批量调价：按范围（商品ID列表/分类/供应商/全部）批量更新商品价格并自动记录价格变更日志
    /// </summary>
    [HttpPost("batch-adjust")]
    public async Task<ApiResponseDto<BatchPriceAdjustResultDto>> BatchAdjustPrice([FromBody] BatchPriceAdjustDto dto)
        => await _appService.BatchAdjustPriceAsync(dto);
}
