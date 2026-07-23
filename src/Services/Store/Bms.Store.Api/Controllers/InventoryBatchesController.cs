using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.InventoryBatches;
using Bms.Store.Application.Services;

namespace Bms.Store.Api.Controllers;

/// <summary>
/// 库存批次管理控制器
/// </summary>
[ApiController]
[Route("api/store/[controller]")]
[Authorize]
public class InventoryBatchesController : ControllerBase
{
    private readonly IInventoryBatchAppService _appService;

    public InventoryBatchesController(IInventoryBatchAppService appService)
    {
        _appService = appService;
    }

    /// <summary>
    /// 获取库存批次分页列表
    /// </summary>
    [HttpGet]
    public async Task<ApiResponseDto<PagedResponseDto<InventoryBatchDto>>> GetList([FromQuery] InventoryBatchQueryDto query)
        => await _appService.GetPagedListAsync(query);

    /// <summary>
    /// 获取库存批次详情
    /// </summary>
    [HttpGet("{id:long}")]
    public async Task<ApiResponseDto<InventoryBatchDto?>> GetById(long id)
        => await _appService.GetByIdAsync(id);

    /// <summary>
    /// 创建库存批次
    /// </summary>
    [HttpPost]
    public async Task<ApiResponseDto<InventoryBatchDto>> Create([FromBody] InventoryBatchCreateDto dto)
        => await _appService.CreateAsync(dto);

    /// <summary>
    /// 更新库存批次
    /// </summary>
    [HttpPut("{id:long}")]
    public async Task<ApiResponseDto<InventoryBatchDto>> Update(long id, [FromBody] InventoryBatchUpdateDto dto)
    {
        dto.Id = id;
        return await _appService.UpdateAsync(dto);
    }

    /// <summary>
    /// 删除库存批次
    /// </summary>
    [HttpDelete("{id:long}")]
    public async Task<ApiResponseDto> Delete(long id)
        => await _appService.DeleteAsync(id);

    /// <summary>
    /// 批量删除库存批次
    /// </summary>
    [HttpPost("batch")]
    public async Task<ApiResponseDto> BatchDelete([FromBody] BatchDeleteRequest request)
        => await _appService.BatchDeleteAsync(request.Ids);

    /// <summary>
    /// 获取效期信息分页列表
    /// </summary>
    [HttpGet("expiries")]
    public async Task<ApiResponseDto<PagedResponseDto<ExpiryDto>>> GetExpiryList([FromQuery] ExpiryQueryDto query)
        => await _appService.GetExpiryListAsync(query);

    /// <summary>
    /// 获取效期预警列表（即将过期或已过期的商品）
    /// </summary>
    [HttpGet("expiryAlerts")]
    public async Task<ApiResponseDto<PagedResponseDto<ExpiryDto>>> GetExpiryAlerts([FromQuery] ExpiryQueryDto query)
        => await _appService.GetExpiryAlertsAsync(query);

    /// <summary>
    /// 按商品ID查询可用效期选项列表（用于 POS 效期选择）。
    /// 返回该商品所有在库且有余量的效期，按到期日期升序排列，近效期优先。
    /// </summary>
    [HttpGet("products/{productId:long}/expiry-options")]
    public async Task<ApiResponseDto<List<ProductExpiryOptionDto>>> GetExpiryOptionsByProductId(long productId)
        => await _appService.GetExpiryOptionsByProductIdAsync(productId);
}
