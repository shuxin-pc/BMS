using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.StoredValues;
using Bms.Store.Application.Services;

namespace Bms.Store.Api.Controllers;

/// <summary>
/// 储值规则管理控制器
/// </summary>
[ApiController]
[Route("api/store/[controller]")]
[Authorize]
public class StoredValueRulesController : ControllerBase
{
    private readonly IStoredValueRuleAppService _appService;

    public StoredValueRulesController(IStoredValueRuleAppService appService)
    {
        _appService = appService;
    }

    /// <summary>
    /// 获取储值规则分页列表
    /// </summary>
    [HttpGet]
    public async Task<ApiResponseDto<PagedResponseDto<StoredValueRuleDto>>> GetList([FromQuery] StoredValueRuleQueryDto query)
        => await _appService.GetPagedListAsync(query);

    /// <summary>
    /// 获取储值规则详情
    /// </summary>
    [HttpGet("{id:long}")]
    public async Task<ApiResponseDto<StoredValueRuleDto?>> GetById(long id)
        => await _appService.GetByIdAsync(id);

    /// <summary>
    /// 创建储值规则
    /// </summary>
    [HttpPost]
    public async Task<ApiResponseDto<StoredValueRuleDto>> Create([FromBody] StoredValueRuleCreateDto dto)
        => await _appService.CreateAsync(dto);

    /// <summary>
    /// 更新储值规则
    /// </summary>
    [HttpPut("{id:long}")]
    public async Task<ApiResponseDto<StoredValueRuleDto>> Update(long id, [FromBody] StoredValueRuleUpdateDto dto)
    {
        dto.Id = id;
        return await _appService.UpdateAsync(dto);
    }

    /// <summary>
    /// 删除储值规则
    /// </summary>
    [HttpDelete("{id:long}")]
    public async Task<ApiResponseDto> Delete(long id)
        => await _appService.DeleteAsync(id);

    /// <summary>
    /// 批量删除储值规则
    /// </summary>
    [HttpPost("batch")]
    public async Task<ApiResponseDto> BatchDelete([FromBody] BatchDeleteRequest request)
        => await _appService.BatchDeleteAsync(request.Ids);

    /// <summary>
    /// 试算充值赠送金额（充值弹窗输入金额时实时展示，与实际充值同一计算口径）
    /// </summary>
    [HttpGet("gift-preview")]
    public async Task<ApiResponseDto<StoredValueGiftPreviewDto>> PreviewGift([FromQuery] decimal amount)
        => await _appService.PreviewGiftAmountAsync(amount);
}
