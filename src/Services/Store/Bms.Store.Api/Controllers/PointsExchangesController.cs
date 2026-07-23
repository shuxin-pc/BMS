using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Points;
using Bms.Store.Application.Services;

namespace Bms.Store.Api.Controllers;

/// <summary>
/// 积分兑换记录管理控制器
/// </summary>
[ApiController]
[Route("api/store/[controller]")]
[Authorize]
public class PointsExchangesController : ControllerBase
{
    private readonly IPointsExchangeAppService _appService;

    public PointsExchangesController(IPointsExchangeAppService appService)
    {
        _appService = appService;
    }

    /// <summary>
    /// 获取积分兑换记录分页列表
    /// </summary>
    [HttpGet]
    public async Task<ApiResponseDto<PagedResponseDto<PointsExchangeDto>>> GetList([FromQuery] PointsExchangeQueryDto query)
        => await _appService.GetPagedListAsync(query);

    /// <summary>
    /// 获取积分兑换记录详情
    /// </summary>
    [HttpGet("{id:long}")]
    public async Task<ApiResponseDto<PointsExchangeDto?>> GetById(long id)
        => await _appService.GetByIdAsync(id);

    /// <summary>
    /// 创建积分兑换记录
    /// </summary>
    [HttpPost]
    public async Task<ApiResponseDto<PointsExchangeDto>> Create([FromBody] PointsExchangeCreateDto dto)
        => await _appService.CreateAsync(dto);

    /// <summary>
    /// 更新积分兑换记录
    /// </summary>
    [HttpPut("{id:long}")]
    public async Task<ApiResponseDto<PointsExchangeDto>> Update(long id, [FromBody] PointsExchangeUpdateDto dto)
    {
        dto.Id = id;
        return await _appService.UpdateAsync(dto);
    }

    /// <summary>
    /// 删除积分兑换记录
    /// </summary>
    [HttpDelete("{id:long}")]
    public async Task<ApiResponseDto> Delete(long id)
        => await _appService.DeleteAsync(id);

    /// <summary>
    /// 批量删除积分兑换记录
    /// </summary>
    [HttpPost("batch")]
    public async Task<ApiResponseDto> BatchDelete([FromBody] BatchDeleteRequest request)
        => await _appService.BatchDeleteAsync(request.Ids);
}
