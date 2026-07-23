using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.PurchaseReturns;
using Bms.Store.Application.Services;

namespace Bms.Store.Api.Controllers;

/// <summary>
/// 采购退货管理控制器
/// </summary>
[ApiController]
[Route("api/store/[controller]")]
[Authorize]
public class PurchaseReturnsController : ControllerBase
{
    private readonly IPurchaseReturnAppService _appService;

    public PurchaseReturnsController(IPurchaseReturnAppService appService)
    {
        _appService = appService;
    }

    /// <summary>
    /// 获取采购退货分页列表
    /// </summary>
    [HttpGet]
    public async Task<ApiResponseDto<PagedResponseDto<PurchaseReturnDto>>> GetList([FromQuery] PurchaseReturnQueryDto query)
        => await _appService.GetPagedListAsync(query);

    /// <summary>
    /// 获取采购退货详情
    /// </summary>
    [HttpGet("{id:long}")]
    public async Task<ApiResponseDto<PurchaseReturnDto?>> GetById(long id)
        => await _appService.GetByIdAsync(id);

    /// <summary>
    /// 创建采购退货
    /// </summary>
    [HttpPost]
    public async Task<ApiResponseDto<PurchaseReturnDto>> Create([FromBody] PurchaseReturnCreateDto dto)
        => await _appService.CreateAsync(dto);

    /// <summary>
    /// 更新采购退货
    /// </summary>
    [HttpPut("{id:long}")]
    public async Task<ApiResponseDto<PurchaseReturnDto>> Update(long id, [FromBody] PurchaseReturnUpdateDto dto)
    {
        dto.Id = id;
        return await _appService.UpdateAsync(dto);
    }

    /// <summary>
    /// 删除采购退货
    /// </summary>
    [HttpDelete("{id:long}")]
    public async Task<ApiResponseDto> Delete(long id)
        => await _appService.DeleteAsync(id);

    /// <summary>
    /// 批量删除采购退货
    /// </summary>
    [HttpPost("batch")]
    public async Task<ApiResponseDto> BatchDelete([FromBody] BatchDeleteRequest request)
        => await _appService.BatchDeleteAsync(request.Ids);
}
