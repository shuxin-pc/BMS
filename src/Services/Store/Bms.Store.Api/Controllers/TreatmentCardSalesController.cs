using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.TreatmentCards;
using Bms.Store.Application.Services;

namespace Bms.Store.Api.Controllers;

/// <summary>
/// 项目卡销售记录管理控制器
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
    /// 获取项目卡销售记录分页列表
    /// </summary>
    [HttpGet]
    public async Task<ApiResponseDto<PagedResponseDto<TreatmentCardSaleDto>>> GetList([FromQuery] TreatmentCardSaleQueryDto query)
        => await _appService.GetPagedListAsync(query);

    /// <summary>
    /// 获取项目卡到期提醒分页列表（含全量预警级别统计）
    /// </summary>
    [HttpGet("expiries")]
    public async Task<ApiResponseDto<TreatmentCardExpiryPageDto>> GetExpiries([FromQuery] TreatmentCardExpiryQueryDto query)
        => await _appService.GetExpiryListAsync(query);

    /// <summary>
    /// 获取项目卡销售记录详情
    /// </summary>
    [HttpGet("{id:long}")]
    public async Task<ApiResponseDto<TreatmentCardSaleDto?>> GetById(long id)
        => await _appService.GetByIdAsync(id);

    /// <summary>
    /// 创建项目卡销售记录
    /// </summary>
    [HttpPost]
    public async Task<ApiResponseDto<TreatmentCardSaleDto>> Create([FromBody] TreatmentCardSaleCreateDto dto)
        => await _appService.CreateAsync(dto);

    /// <summary>
    /// 更新项目卡销售记录
    /// </summary>
    [HttpPut("{id:long}")]
    public async Task<ApiResponseDto<TreatmentCardSaleDto>> Update(long id, [FromBody] TreatmentCardSaleUpdateDto dto)
    {
        dto.Id = id;
        return await _appService.UpdateAsync(dto);
    }

    /// <summary>
    /// 删除项目卡销售记录
    /// </summary>
    [HttpDelete("{id:long}")]
    public async Task<ApiResponseDto> Delete(long id)
        => await _appService.DeleteAsync(id);

    /// <summary>
    /// 批量删除项目卡销售记录
    /// </summary>
    [HttpPost("batch")]
    public async Task<ApiResponseDto> BatchDelete([FromBody] BatchDeleteRequest request)
        => await _appService.BatchDeleteAsync(request.Ids);

    /// <summary>
    /// 退卡（规则6）
    /// 全额冲减发卡门店销售业绩，已发生的核销业绩不冲回
    /// 退卡金额 = 售价 - 已核销金额
    /// </summary>
    [HttpPost("{id:long}/refund")]
    public async Task<ApiResponseDto<TreatmentCardSaleRefundResultDto>> Refund(long id, [FromBody] TreatmentCardSaleRefundDto dto)
    {
        dto.Id = id;
        return await _appService.RefundAsync(dto);
    }
}
