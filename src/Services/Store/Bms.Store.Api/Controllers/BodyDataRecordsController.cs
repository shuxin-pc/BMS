using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Customers;
using Bms.Store.Application.Services;

namespace Bms.Store.Api.Controllers;

[ApiController]
[Route("api/store/[controller]")]
[Authorize]
public class BodyDataRecordsController : ControllerBase
{
    private readonly IBodyDataRecordAppService _appService;

    public BodyDataRecordsController(IBodyDataRecordAppService appService)
    {
        _appService = appService;
    }

    [HttpGet]
    public async Task<ApiResponseDto<PagedResponseDto<BodyDataRecordDto>>> GetList([FromQuery] BodyDataRecordQueryDto query)
        => await _appService.GetPagedListAsync(query);

    [HttpGet("{id:long}")]
    public async Task<ApiResponseDto<BodyDataRecordDto?>> GetById(long id)
        => await _appService.GetByIdAsync(id);

    [HttpPost]
    public async Task<ApiResponseDto<BodyDataRecordDto>> Create([FromBody] BodyDataRecordCreateDto dto)
        => await _appService.CreateAsync(dto);

    [HttpPut("{id:long}")]
    public async Task<ApiResponseDto<BodyDataRecordDto>> Update(long id, [FromBody] BodyDataRecordUpdateDto dto)
    {
        dto.Id = id;
        return await _appService.UpdateAsync(dto);
    }

    [HttpDelete("{id:long}")]
    public async Task<ApiResponseDto> Delete(long id)
        => await _appService.DeleteAsync(id);

    [HttpPost("batch")]
    public async Task<ApiResponseDto> BatchDelete([FromBody] BatchDeleteRequest request)
        => await _appService.BatchDeleteAsync(request.Ids);

    /// <summary>
    /// 获取客户最近一次身体数据记录
    /// </summary>
    [HttpGet("latest/{customerId:long}")]
    public async Task<ApiResponseDto<BodyDataRecordDto?>> GetLatest(long customerId)
        => await _appService.GetLatestAsync(customerId);

    /// <summary>
    /// 按时间范围获取身体数据趋势
    /// </summary>
    [HttpGet("trend/{customerId:long}")]
    public async Task<ApiResponseDto<BodyDataTrendDto>> GetTrend(
        long customerId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        => await _appService.GetTrendAsync(customerId, startDate, endDate);

    /// <summary>
    /// 对比两个时间点的身体数据差异
    /// </summary>
    [HttpGet("comparison/{customerId:long}")]
    public async Task<ApiResponseDto<BodyDataComparisonDto>> GetComparison(
        long customerId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        => await _appService.GetComparisonAsync(customerId, startDate, endDate);
}
