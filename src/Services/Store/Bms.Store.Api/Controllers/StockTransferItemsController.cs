using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.StockTransfers;
using Bms.Store.Application.Services;

namespace Bms.Store.Api.Controllers;

/// <summary>
/// 库存调拨单明细管理控制器
/// </summary>
[ApiController]
[Route("api/store/[controller]")]
[Authorize]
public class StockTransferItemsController : ControllerBase
{
    private readonly IStockTransferItemAppService _appService;

    public StockTransferItemsController(IStockTransferItemAppService appService)
    {
        _appService = appService;
    }

    /// <summary>
    /// 获取库存调拨单明细分页列表
    /// </summary>
    [HttpGet]
    public async Task<ApiResponseDto<PagedResponseDto<StockTransferItemDto>>> GetList([FromQuery] StockTransferItemQueryDto query)
        => await _appService.GetPagedListAsync(query);

    /// <summary>
    /// 获取库存调拨单明细详情
    /// </summary>
    [HttpGet("{id:long}")]
    public async Task<ApiResponseDto<StockTransferItemDto?>> GetById(long id)
        => await _appService.GetByIdAsync(id);

    /// <summary>
    /// 创建库存调拨单明细
    /// </summary>
    [HttpPost]
    public async Task<ApiResponseDto<StockTransferItemDto>> Create([FromBody] StockTransferItemCreateDto dto)
        => await _appService.CreateAsync(dto);

    /// <summary>
    /// 更新库存调拨单明细
    /// </summary>
    [HttpPut("{id:long}")]
    public async Task<ApiResponseDto<StockTransferItemDto>> Update(long id, [FromBody] StockTransferItemUpdateDto dto)
    {
        dto.Id = id;
        return await _appService.UpdateAsync(dto);
    }

    /// <summary>
    /// 删除库存调拨单明细
    /// </summary>
    [HttpDelete("{id:long}")]
    public async Task<ApiResponseDto> Delete(long id)
        => await _appService.DeleteAsync(id);

    /// <summary>
    /// 批量删除库存调拨单明细
    /// </summary>
    [HttpPost("batch")]
    public async Task<ApiResponseDto> BatchDelete([FromBody] BatchDeleteRequest request)
        => await _appService.BatchDeleteAsync(request.Ids);
}
