using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.TreatmentCardTransfers;
using Bms.Store.Application.Services;

namespace Bms.Store.Api.Controllers;

/// <summary>
/// 项目卡转让管理控制器
/// </summary>
[ApiController]
[Route("api/store/[controller]")]
[Authorize]
public class TreatmentCardTransfersController : ControllerBase
{
    private readonly ITreatmentCardTransferAppService _appService;

    public TreatmentCardTransfersController(ITreatmentCardTransferAppService appService)
    {
        _appService = appService;
    }

    /// <summary>
    /// 获取项目卡转让记录分页列表
    /// </summary>
    [HttpGet]
    public async Task<ApiResponseDto<PagedResponseDto<TreatmentCardTransferDto>>> GetList([FromQuery] TreatmentCardTransferQueryDto query)
        => await _appService.GetPagedListAsync(query);

    /// <summary>
    /// 获取可转让的项目卡销售记录选项（转卡弹窗选择用）
    /// 仅返回状态有效且有剩余次数的卡销售记录
    /// </summary>
    [HttpGet("transferable-card-sales")]
    public async Task<ApiResponseDto<List<TreatmentCardTransferOptionDto>>> GetTransferableCardSales()
        => await _appService.GetTransferableCardSalesAsync();

    /// <summary>
    /// 获取项目卡转让记录详情
    /// </summary>
    [HttpGet("{id:long}")]
    public async Task<ApiResponseDto<TreatmentCardTransferDto?>> GetById(long id)
        => await _appService.GetByIdAsync(id);

    /// <summary>
    /// 创建项目卡转让记录
    /// </summary>
    [HttpPost]
    public async Task<ApiResponseDto<TreatmentCardTransferDto>> Create([FromBody] TreatmentCardTransferCreateDto dto)
        => await _appService.CreateAsync(dto);

    /// <summary>
    /// 更新项目卡转让记录
    /// </summary>
    [HttpPut("{id:long}")]
    public async Task<ApiResponseDto<TreatmentCardTransferDto>> Update(long id, [FromBody] TreatmentCardTransferUpdateDto dto)
    {
        dto.Id = id;
        return await _appService.UpdateAsync(dto);
    }

    /// <summary>
    /// 删除项目卡转让记录
    /// </summary>
    [HttpDelete("{id:long}")]
    public async Task<ApiResponseDto> Delete(long id)
        => await _appService.DeleteAsync(id);

    /// <summary>
    /// 批量删除项目卡转让记录
    /// </summary>
    [HttpPost("batch")]
    public async Task<ApiResponseDto> BatchDelete([FromBody] BatchDeleteRequest request)
        => await _appService.BatchDeleteAsync(request.Ids);
}
