using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.TreatmentCards;
using Bms.Store.Application.Services;

namespace Bms.Store.Api.Controllers;

/// <summary>
/// 疗程卡配置管理控制器
/// </summary>
[ApiController]
[Route("api/store/[controller]")]
[Authorize]
public class TreatmentCardsController : ControllerBase
{
    private readonly ITreatmentCardAppService _appService;

    public TreatmentCardsController(ITreatmentCardAppService appService)
    {
        _appService = appService;
    }

    /// <summary>
    /// 获取疗程卡配置分页列表
    /// </summary>
    [HttpGet]
    public async Task<ApiResponseDto<PagedResponseDto<TreatmentCardDto>>> GetList([FromQuery] TreatmentCardQueryDto query)
        => await _appService.GetPagedListAsync(query);

    /// <summary>
    /// 获取疗程卡配置详情
    /// </summary>
    [HttpGet("{id:long}")]
    public async Task<ApiResponseDto<TreatmentCardDto?>> GetById(long id)
        => await _appService.GetByIdAsync(id);

    /// <summary>
    /// 创建疗程卡配置
    /// </summary>
    [HttpPost]
    public async Task<ApiResponseDto<TreatmentCardDto>> Create([FromBody] TreatmentCardCreateDto dto)
        => await _appService.CreateAsync(dto);

    /// <summary>
    /// 更新疗程卡配置
    /// </summary>
    [HttpPut("{id:long}")]
    public async Task<ApiResponseDto<TreatmentCardDto>> Update(long id, [FromBody] TreatmentCardUpdateDto dto)
    {
        dto.Id = id;
        return await _appService.UpdateAsync(dto);
    }

    /// <summary>
    /// 删除疗程卡配置
    /// </summary>
    [HttpDelete("{id:long}")]
    public async Task<ApiResponseDto> Delete(long id)
        => await _appService.DeleteAsync(id);

    /// <summary>
    /// 批量删除疗程卡配置
    /// </summary>
    [HttpPost("batch")]
    public async Task<ApiResponseDto> BatchDelete([FromBody] BatchDeleteRequest request)
        => await _appService.BatchDeleteAsync(request.Ids);
}
