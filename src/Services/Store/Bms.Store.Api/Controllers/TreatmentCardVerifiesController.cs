using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.TreatmentCards;
using Bms.Store.Application.Services;

namespace Bms.Store.Api.Controllers;

/// <summary>
/// 项目卡核销记录管理控制器
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
    /// 获取项目卡核销记录分页列表
    /// </summary>
    [HttpGet]
    public async Task<ApiResponseDto<PagedResponseDto<TreatmentCardVerifyDto>>> GetList([FromQuery] TreatmentCardVerifyQueryDto query)
        => await _appService.GetPagedListAsync(query);

    /// <summary>
    /// 获取项目卡核销记录详情
    /// </summary>
    [HttpGet("{id:long}")]
    public async Task<ApiResponseDto<TreatmentCardVerifyDto?>> GetById(long id)
        => await _appService.GetByIdAsync(id);

    /// <summary>
    /// 创建项目卡核销记录
    /// </summary>
    [HttpPost]
    public async Task<ApiResponseDto<TreatmentCardVerifyDto>> Create([FromBody] TreatmentCardVerifyCreateDto dto)
        => await _appService.CreateAsync(dto);

    /// <summary>
    /// 更新项目卡核销记录
    /// </summary>
    [HttpPut("{id:long}")]
    public async Task<ApiResponseDto<TreatmentCardVerifyDto>> Update(long id, [FromBody] TreatmentCardVerifyUpdateDto dto)
    {
        dto.Id = id;
        return await _appService.UpdateAsync(dto);
    }

    /// <summary>
    /// 核销冲正（规则7）
    /// 通过状态机实现，不物理删除核销记录
    /// 冲正时恢复项目卡剩余次数、取消关联订单、冲减业绩统计
    /// </summary>
    [HttpPost("{id:long}/reverse")]
    public async Task<ApiResponseDto> Reverse(long id, [FromBody] TreatmentCardVerifyReverseDto dto)
    {
        dto.Id = id;
        return await _appService.ReverseAsync(dto);
    }
}
