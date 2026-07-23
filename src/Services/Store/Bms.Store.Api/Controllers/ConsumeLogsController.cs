using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Customers;
using Bms.Store.Application.Services;

namespace Bms.Store.Api.Controllers;

/// <summary>
/// 消费记录管理控制器
/// </summary>
[ApiController]
[Route("api/store/[controller]")]
[Authorize]
public class ConsumeLogsController : ControllerBase
{
    private readonly IConsumeLogAppService _appService;

    public ConsumeLogsController(IConsumeLogAppService appService)
    {
        _appService = appService;
    }

    /// <summary>
    /// 获取消费记录分页列表
    /// </summary>
    [HttpGet]
    public async Task<ApiResponseDto<PagedResponseDto<ConsumeLogDto>>> GetList([FromQuery] ConsumeLogQueryDto query)
        => await _appService.GetPagedListAsync(query);

    /// <summary>
    /// 获取消费记录详情
    /// </summary>
    [HttpGet("{id:long}")]
    public async Task<ApiResponseDto<ConsumeLogDto?>> GetById(long id)
        => await _appService.GetByIdAsync(id);

    /// <summary>
    /// 创建消费记录
    /// </summary>
    [HttpPost]
    public async Task<ApiResponseDto<ConsumeLogDto>> Create([FromBody] ConsumeLogCreateDto dto)
        => await _appService.CreateAsync(dto);

    /// <summary>
    /// 更新消费记录
    /// </summary>
    [HttpPut("{id:long}")]
    public async Task<ApiResponseDto<ConsumeLogDto>> Update(long id, [FromBody] ConsumeLogUpdateDto dto)
    {
        dto.Id = id;
        return await _appService.UpdateAsync(dto);
    }

    /// <summary>
    /// 删除消费记录
    /// </summary>
    [HttpDelete("{id:long}")]
    public async Task<ApiResponseDto> Delete(long id)
        => await _appService.DeleteAsync(id);

    /// <summary>
    /// 批量删除消费记录
    /// </summary>
    [HttpPost("batch")]
    public async Task<ApiResponseDto> BatchDelete([FromBody] BatchDeleteRequest request)
        => await _appService.BatchDeleteAsync(request.Ids);
}
