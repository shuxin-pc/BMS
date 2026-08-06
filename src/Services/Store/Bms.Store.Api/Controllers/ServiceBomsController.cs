using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.ServiceBoms;
using Bms.Store.Application.Services;

namespace Bms.Store.Api.Controllers;

/// <summary>
/// 服务BOM管理控制器
/// </summary>
[ApiController]
[Route("api/store/[controller]")]
[Authorize]
public class ServiceBomsController : ControllerBase
{
    private readonly IServiceBomAppService _appService;

    public ServiceBomsController(IServiceBomAppService appService)
    {
        _appService = appService;
    }

    [HttpGet]
    public async Task<ApiResponseDto<PagedResponseDto<ServiceBomDto>>> GetList([FromQuery] ServiceBomQueryDto query)
        => await _appService.GetPagedListAsync(query);

    [HttpGet("{id:long}")]
    public async Task<ApiResponseDto<ServiceBomDto?>> GetById(long id)
        => await _appService.GetByIdAsync(id);

    [HttpPost]
    public async Task<ApiResponseDto<ServiceBomDto>> Create([FromBody] ServiceBomCreateDto dto)
        => await _appService.CreateAsync(dto);

    [HttpPut("{id:long}")]
    public async Task<ApiResponseDto<ServiceBomDto>> Update(long id, [FromBody] ServiceBomUpdateDto dto)
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
    /// 获取服务项目选项列表（用于BOM下拉选择）
    /// </summary>
    [HttpGet("service-product-options")]
    public async Task<ApiResponseDto<List<ServiceProductOptionDto>>> GetServiceProductOptions()
        => await _appService.GetServiceProductOptionsAsync();

    /// <summary>
    /// 获取耗材商品选项列表（用于BOM下拉选择，仅 type=3 耗材）
    /// </summary>
    [HttpGet("consumable-options")]
    public async Task<ApiResponseDto<List<ConsumableOptionDto>>> GetConsumableOptions()
        => await _appService.GetConsumableOptionsAsync();
}
