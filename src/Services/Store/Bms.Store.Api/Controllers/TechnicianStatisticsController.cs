using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Technicians;
using Bms.Store.Application.Services;

namespace Bms.Store.Api.Controllers;

/// <summary>
/// 技师统计管理控制器
/// </summary>
[ApiController]
[Route("api/store/[controller]")]
[Authorize]
public class TechnicianStatisticsController : ControllerBase
{
    private readonly ITechnicianStatisticAppService _appService;

    public TechnicianStatisticsController(ITechnicianStatisticAppService appService)
    {
        _appService = appService;
    }

    /// <summary>
    /// 获取技师统计分页列表
    /// </summary>
    [HttpGet]
    public async Task<ApiResponseDto<PagedResponseDto<TechnicianStatisticDto>>> GetList([FromQuery] TechnicianStatisticQueryDto query)
        => await _appService.GetPagedListAsync(query);

    /// <summary>
    /// 获取技师业绩统计报表（从 OrderItem 实时聚合，仅商家技师）
    /// 对应需求 B4.5
    /// 纯平台技师门店返回 IsPurePlatformStore=true + 空列表，业绩由平台统一统计
    /// </summary>
    [HttpGet("report")]
    public async Task<ApiResponseDto<TechnicianStatReportDto>> GetReport([FromQuery] TechnicianStatisticQueryDto query)
        => await _appService.GetReportAsync(query);

    /// <summary>
    /// 获取技师统计详情
    /// </summary>
    [HttpGet("{id:long}")]
    public async Task<ApiResponseDto<TechnicianStatisticDto?>> GetById(long id)
        => await _appService.GetByIdAsync(id);

    /// <summary>
    /// 创建技师统计
    /// </summary>
    [HttpPost]
    public async Task<ApiResponseDto<TechnicianStatisticDto>> Create([FromBody] TechnicianStatisticCreateDto dto)
        => await _appService.CreateAsync(dto);

    /// <summary>
    /// 更新技师统计
    /// </summary>
    [HttpPut("{id:long}")]
    public async Task<ApiResponseDto<TechnicianStatisticDto>> Update(long id, [FromBody] TechnicianStatisticUpdateDto dto)
    {
        dto.Id = id;
        return await _appService.UpdateAsync(dto);
    }

    /// <summary>
    /// 删除技师统计
    /// </summary>
    [HttpDelete("{id:long}")]
    public async Task<ApiResponseDto> Delete(long id)
        => await _appService.DeleteAsync(id);

    /// <summary>
    /// 批量删除技师统计
    /// </summary>
    [HttpPost("batch")]
    public async Task<ApiResponseDto> BatchDelete([FromBody] BatchDeleteRequest request)
        => await _appService.BatchDeleteAsync(request.Ids);
}
