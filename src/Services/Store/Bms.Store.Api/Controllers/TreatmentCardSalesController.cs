using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.TreatmentCards;
using Bms.Store.Application.Services;

namespace Bms.Store.Api.Controllers;

/// <summary>
/// 疗程卡销售记录管理控制器
/// </summary>
[ApiController]
[Route("api/store/[controller]")]
[Authorize]
public class TreatmentCardSalesController : ControllerBase
{
    private readonly ITreatmentCardSaleAppService _appService;

    public TreatmentCardSalesController(ITreatmentCardSaleAppService appService)
    {
        _appService = appService;
    }

    /// <summary>
    /// 获取疗程卡销售记录分页列表
    /// </summary>
    [HttpGet]
    public async Task<ApiResponseDto<PagedResponseDto<TreatmentCardSaleDto>>> GetList([FromQuery] TreatmentCardSaleQueryDto query)
        => await _appService.GetPagedListAsync(query);

    /// <summary>
    /// 获取疗程卡到期提醒分页列表
    /// </summary>
    [HttpGet("expiries")]
    public async Task<ApiResponseDto<PagedResponseDto<TreatmentCardExpiryDto>>> GetExpiries([FromQuery] TreatmentCardExpiryQueryDto query)
        => await _appService.GetExpiryListAsync(query);

    /// <summary>
    /// 获取疗程卡销售记录详情
    /// </summary>
    [HttpGet("{id:long}")]
    public async Task<ApiResponseDto<TreatmentCardSaleDto?>> GetById(long id)
        => await _appService.GetByIdAsync(id);

    /// <summary>
    /// 创建疗程卡销售记录
    /// </summary>
    [HttpPost]
    public async Task<ApiResponseDto<TreatmentCardSaleDto>> Create([FromBody] TreatmentCardSaleCreateDto dto)
        => await _appService.CreateAsync(dto);

    /// <summary>
    /// 更新疗程卡销售记录
    /// </summary>
    [HttpPut("{id:long}")]
    public async Task<ApiResponseDto<TreatmentCardSaleDto>> Update(long id, [FromBody] TreatmentCardSaleUpdateDto dto)
    {
        dto.Id = id;
        return await _appService.UpdateAsync(dto);
    }

    /// <summary>
    /// 删除疗程卡销售记录
    /// </summary>
    [HttpDelete("{id:long}")]
    public async Task<ApiResponseDto> Delete(long id)
        => await _appService.DeleteAsync(id);

    /// <summary>
    /// 批量删除疗程卡销售记录
    /// </summary>
    [HttpPost("batch")]
    public async Task<ApiResponseDto> BatchDelete([FromBody] BatchDeleteRequest request)
        => await _appService.BatchDeleteAsync(request.Ids);
}
