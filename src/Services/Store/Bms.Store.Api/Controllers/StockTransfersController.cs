using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.StockTransfers;
using Bms.Store.Application.Services;

namespace Bms.Store.Api.Controllers;

/// <summary>
/// 库存调拨单管理控制器
/// </summary>
[ApiController]
[Route("api/store/[controller]")]
[Authorize]
public class StockTransfersController : ControllerBase
{
    private readonly IStockTransferAppService _appService;

    public StockTransfersController(IStockTransferAppService appService)
    {
        _appService = appService;
    }

    /// <summary>
    /// 获取库存调拨单分页列表
    /// </summary>
    [HttpGet]
    public async Task<ApiResponseDto<PagedResponseDto<StockTransferDto>>> GetList([FromQuery] StockTransferQueryDto query)
        => await _appService.GetPagedListAsync(query);

    /// <summary>
    /// 获取库存调拨单详情
    /// </summary>
    [HttpGet("{id:long}")]
    public async Task<ApiResponseDto<StockTransferDto?>> GetById(long id)
        => await _appService.GetByIdAsync(id);

    /// <summary>
    /// 创建库存调拨单
    /// </summary>
    [HttpPost]
    public async Task<ApiResponseDto<StockTransferDto>> Create([FromBody] StockTransferCreateDto dto)
        => await _appService.CreateAsync(dto);

    /// <summary>
    /// 更新库存调拨单
    /// </summary>
    [HttpPut("{id:long}")]
    public async Task<ApiResponseDto<StockTransferDto>> Update(long id, [FromBody] StockTransferUpdateDto dto)
    {
        dto.Id = id;
        return await _appService.UpdateAsync(dto);
    }

    /// <summary>
    /// 删除库存调拨单
    /// </summary>
    [HttpDelete("{id:long}")]
    public async Task<ApiResponseDto> Delete(long id)
        => await _appService.DeleteAsync(id);

    /// <summary>
    /// 批量删除库存调拨单
    /// </summary>
    [HttpPost("batch")]
    public async Task<ApiResponseDto> BatchDelete([FromBody] BatchDeleteRequest request)
        => await _appService.BatchDeleteAsync(request.Ids);

    /// <summary>
    /// 执行调拨（从调出门店扣减库存，向调入门店增加库存，状态转为已调入）
    /// </summary>
    [HttpPost("{id:long}/execute")]
    public async Task<ApiResponseDto> Execute(long id)
        => await _appService.ExecuteAsync(id);

    /// <summary>
    /// 取消调拨单（草稿转已取消，已调入的不可取消）
    /// </summary>
    [HttpPost("{id:long}/cancel")]
    public async Task<ApiResponseDto> Cancel(long id, [FromQuery] string? reason)
        => await _appService.CancelAsync(id, reason);
}
