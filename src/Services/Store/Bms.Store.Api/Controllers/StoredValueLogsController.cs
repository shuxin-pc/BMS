using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.StoredValues;
using Bms.Store.Application.Services;

namespace Bms.Store.Api.Controllers;

/// <summary>
/// 储值流水管理控制器
/// </summary>
[ApiController]
[Route("api/store/[controller]")]
[Authorize]
public class StoredValueLogsController : ControllerBase
{
    private readonly IStoredValueLogAppService _appService;

    public StoredValueLogsController(IStoredValueLogAppService appService)
    {
        _appService = appService;
    }

    /// <summary>
    /// 获取储值流水分页列表
    /// </summary>
    [HttpGet]
    public async Task<ApiResponseDto<PagedResponseDto<StoredValueLogDto>>> GetList([FromQuery] StoredValueLogQueryDto query)
        => await _appService.GetPagedListAsync(query);

    /// <summary>
    /// 储值现金流统计（G7.3）：按日期范围统计新增储值/消费/退款/沉淀资金
    /// </summary>
    [HttpGet("cash-flow")]
    public async Task<ApiResponseDto<StoredValueCashFlowDto>> GetCashFlow(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
        => await _appService.GetCashFlowAsync(startDate, endDate);

    /// <summary>
    /// 获取储值流水详情
    /// </summary>
    [HttpGet("{id:long}")]
    public async Task<ApiResponseDto<StoredValueLogDto?>> GetById(long id)
        => await _appService.GetByIdAsync(id);

    /// <summary>
    /// 创建储值流水
    /// </summary>
    [HttpPost]
    public async Task<ApiResponseDto<StoredValueLogDto>> Create([FromBody] StoredValueLogCreateDto dto)
        => await _appService.CreateAsync(dto);

    /// <summary>
    /// 更新储值流水
    /// </summary>
    [HttpPut("{id:long}")]
    public async Task<ApiResponseDto<StoredValueLogDto>> Update(long id, [FromBody] StoredValueLogUpdateDto dto)
    {
        dto.Id = id;
        return await _appService.UpdateAsync(dto);
    }

    /// <summary>
    /// 删除储值流水
    /// </summary>
    [HttpDelete("{id:long}")]
    public async Task<ApiResponseDto> Delete(long id)
        => await _appService.DeleteAsync(id);

    /// <summary>
    /// 批量删除储值流水
    /// </summary>
    [HttpPost("batch")]
    public async Task<ApiResponseDto> BatchDelete([FromBody] BatchDeleteRequest request)
        => await _appService.BatchDeleteAsync(request.Ids);
}
