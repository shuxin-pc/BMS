using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.PurchaseOrders;
using Bms.Store.Application.Services;

namespace Bms.Store.Api.Controllers;

/// <summary>
/// 采购订单管理控制器
/// </summary>
[ApiController]
[Route("api/store/[controller]")]
[Authorize]
public class PurchaseOrdersController : ControllerBase
{
    private readonly IPurchaseOrderAppService _appService;

    public PurchaseOrdersController(IPurchaseOrderAppService appService)
    {
        _appService = appService;
    }

    /// <summary>
    /// 获取采购订单分页列表
    /// </summary>
    [HttpGet]
    public async Task<ApiResponseDto<PagedResponseDto<PurchaseOrderDto>>> GetList([FromQuery] PurchaseOrderQueryDto query)
        => await _appService.GetPagedListAsync(query);

    /// <summary>
    /// 获取采购订单详情
    /// </summary>
    [HttpGet("{id:long}")]
    public async Task<ApiResponseDto<PurchaseOrderDto?>> GetById(long id)
        => await _appService.GetByIdAsync(id);

    /// <summary>
    /// 创建采购订单
    /// </summary>
    [HttpPost]
    public async Task<ApiResponseDto<PurchaseOrderDto>> Create([FromBody] PurchaseOrderCreateDto dto)
        => await _appService.CreateAsync(dto);

    /// <summary>
    /// 更新采购订单
    /// </summary>
    [HttpPut("{id:long}")]
    public async Task<ApiResponseDto<PurchaseOrderDto>> Update(long id, [FromBody] PurchaseOrderUpdateDto dto)
    {
        dto.Id = id;
        return await _appService.UpdateAsync(dto);
    }

    /// <summary>
    /// 删除采购订单
    /// </summary>
    [HttpDelete("{id:long}")]
    public async Task<ApiResponseDto> Delete(long id)
        => await _appService.DeleteAsync(id);

    /// <summary>
    /// 批量删除采购订单
    /// </summary>
    [HttpPost("batch")]
    public async Task<ApiResponseDto> BatchDelete([FromBody] BatchDeleteRequest request)
        => await _appService.BatchDeleteAsync(request.Ids);
}
