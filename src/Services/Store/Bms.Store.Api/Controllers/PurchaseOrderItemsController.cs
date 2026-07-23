using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.PurchaseOrders;
using Bms.Store.Application.Services;

namespace Bms.Store.Api.Controllers;

/// <summary>
/// 采购订单明细管理控制器
/// </summary>
[ApiController]
[Route("api/store/[controller]")]
[Authorize]
public class PurchaseOrderItemsController : ControllerBase
{
    private readonly IPurchaseOrderItemAppService _appService;

    public PurchaseOrderItemsController(IPurchaseOrderItemAppService appService)
    {
        _appService = appService;
    }

    /// <summary>
    /// 获取采购订单明细分页列表
    /// </summary>
    [HttpGet]
    public async Task<ApiResponseDto<PagedResponseDto<PurchaseOrderItemDto>>> GetList([FromQuery] PurchaseOrderItemQueryDto query)
        => await _appService.GetPagedListAsync(query);

    /// <summary>
    /// 获取采购订单明细详情
    /// </summary>
    [HttpGet("{id:long}")]
    public async Task<ApiResponseDto<PurchaseOrderItemDto?>> GetById(long id)
        => await _appService.GetByIdAsync(id);

    /// <summary>
    /// 创建采购订单明细
    /// </summary>
    [HttpPost]
    public async Task<ApiResponseDto<PurchaseOrderItemDto>> Create([FromBody] PurchaseOrderItemCreateDto dto)
        => await _appService.CreateAsync(dto);

    /// <summary>
    /// 更新采购订单明细
    /// </summary>
    [HttpPut("{id:long}")]
    public async Task<ApiResponseDto<PurchaseOrderItemDto>> Update(long id, [FromBody] PurchaseOrderItemUpdateDto dto)
    {
        dto.Id = id;
        return await _appService.UpdateAsync(dto);
    }

    /// <summary>
    /// 删除采购订单明细
    /// </summary>
    [HttpDelete("{id:long}")]
    public async Task<ApiResponseDto> Delete(long id)
        => await _appService.DeleteAsync(id);

    /// <summary>
    /// 批量删除采购订单明细
    /// </summary>
    [HttpPost("batch")]
    public async Task<ApiResponseDto> BatchDelete([FromBody] BatchDeleteRequest request)
        => await _appService.BatchDeleteAsync(request.Ids);
}
