using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.TreatmentCards;
using Bms.Store.Application.Services;

namespace Bms.Store.Api.Controllers;

/// <summary>
/// 疗程卡核销记录管理控制器
/// </summary>
[ApiController]
[Route("api/store/[controller]")]
[Authorize]
public class TreatmentCardVerifiesController : ControllerBase
{
    private readonly ITreatmentCardVerifyAppService _appService;

    public TreatmentCardVerifiesController(ITreatmentCardVerifyAppService appService)
    {
        _appService = appService;
    }

    /// <summary>
    /// 获取疗程卡核销记录分页列表
    /// </summary>
    [HttpGet]
    public async Task<ApiResponseDto<PagedResponseDto<TreatmentCardVerifyDto>>> GetList([FromQuery] TreatmentCardVerifyQueryDto query)
        => await _appService.GetPagedListAsync(query);

    /// <summary>
    /// 获取疗程卡核销记录详情
    /// </summary>
    [HttpGet("{id:long}")]
    public async Task<ApiResponseDto<TreatmentCardVerifyDto?>> GetById(long id)
        => await _appService.GetByIdAsync(id);

    /// <summary>
    /// 创建疗程卡核销记录
    /// </summary>
    [HttpPost]
    public async Task<ApiResponseDto<TreatmentCardVerifyDto>> Create([FromBody] TreatmentCardVerifyCreateDto dto)
        => await _appService.CreateAsync(dto);

    /// <summary>
    /// 更新疗程卡核销记录
    /// </summary>
    [HttpPut("{id:long}")]
    public async Task<ApiResponseDto<TreatmentCardVerifyDto>> Update(long id, [FromBody] TreatmentCardVerifyUpdateDto dto)
    {
        dto.Id = id;
        return await _appService.UpdateAsync(dto);
    }
}
