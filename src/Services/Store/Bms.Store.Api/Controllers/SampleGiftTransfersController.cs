using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.SampleGiftTransfers;
using Bms.Store.Application.Services;

namespace Bms.Store.Api.Controllers;

/// <summary>
/// 样品赠品调拨单管理控制器
/// 仅支持样品(4)/赠品(5)商品的跨门店调拨
/// </summary>
[ApiController]
[Route("api/store/[controller]")]
[Authorize]
public class SampleGiftTransfersController : ControllerBase
{
    private readonly ISampleGiftTransferAppService _appService;

    public SampleGiftTransfersController(ISampleGiftTransferAppService appService)
    {
        _appService = appService;
    }

    /// <summary>
    /// 获取样品赠品调拨单分页列表
    /// </summary>
    [HttpGet]
    public async Task<ApiResponseDto<PagedResponseDto<SampleGiftTransferDto>>> GetList([FromQuery] SampleGiftTransferQueryDto query)
        => await _appService.GetPagedListAsync(query);

    /// <summary>
    /// 获取样品赠品调拨单详情
    /// </summary>
    [HttpGet("{id:long}")]
    public async Task<ApiResponseDto<SampleGiftTransferDto?>> GetById(long id)
        => await _appService.GetByIdAsync(id);

    /// <summary>
    /// 创建样品赠品调拨单（待调出状态，不调整库存）
    /// </summary>
    [HttpPost]
    public async Task<ApiResponseDto<SampleGiftTransferDto>> Create([FromBody] SampleGiftTransferCreateDto dto)
        => await _appService.CreateAsync(dto);

    /// <summary>
    /// 更新样品赠品调拨单
    /// </summary>
    [HttpPut("{id:long}")]
    public async Task<ApiResponseDto<SampleGiftTransferDto>> Update(long id, [FromBody] SampleGiftTransferUpdateDto dto)
    {
        dto.Id = id;
        return await _appService.UpdateAsync(dto);
    }

    /// <summary>
    /// 删除样品赠品调拨单（仅待调出状态可删除）
    /// </summary>
    [HttpDelete("{id:long}")]
    public async Task<ApiResponseDto> Delete(long id)
        => await _appService.DeleteAsync(id);

    /// <summary>
    /// 批量删除样品赠品调拨单
    /// </summary>
    [HttpPost("batch")]
    public async Task<ApiResponseDto> BatchDelete([FromBody] BatchDeleteRequest request)
        => await _appService.BatchDeleteAsync(request.Ids);

    /// <summary>
    /// 执行调拨（事务内调出门店扣减库存+调入门店增加库存，状态转为已调入）
    /// </summary>
    [HttpPost("{id:long}/execute")]
    public async Task<ApiResponseDto> Execute(long id)
        => await _appService.ExecuteAsync(id);

    /// <summary>
    /// 取消调拨单（待调出转已取消，已调入的不可取消，需走反向调拨单）
    /// </summary>
    [HttpPost("{id:long}/cancel")]
    public async Task<ApiResponseDto> Cancel(long id)
        => await _appService.CancelAsync(id);

    /// <summary>
    /// 获取调出门店的样品/赠品商品选项（仅返回 Type∈{4,5} 且 Stock > 0 的商品）
    /// 用于新增调拨时商品下拉选择，支持跨门店查询
    /// </summary>
    [HttpGet("from-store-products")]
    public async Task<ApiResponseDto<List<SampleGiftTransferProductOptionDto>>> GetFromStoreProducts([FromQuery] long fromStoreId)
        => await _appService.GetFromStoreProductsAsync(fromStoreId);

    /// <summary>
    /// 获取调出门店指定商品的在库批次列表（按过期日期升序，FEFO）
    /// 用于手动指定批次模式下的批次下拉选择
    /// </summary>
    [HttpGet("from-store-products/{productId:long}/batches")]
    public async Task<ApiResponseDto<List<SampleGiftTransferBatchOptionDto>>> GetFromStoreProductBatches(long productId, [FromQuery] long fromStoreId)
        => await _appService.GetFromStoreProductBatchesAsync(fromStoreId, productId);
}
