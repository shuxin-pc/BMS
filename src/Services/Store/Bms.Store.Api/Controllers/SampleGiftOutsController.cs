using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.SampleGifts;
using Bms.Store.Application.Services;

namespace Bms.Store.Api.Controllers;

/// <summary>
/// 赠品出库记录管理控制器
/// </summary>
[ApiController]
[Route("api/store/[controller]")]
[Authorize]
public class SampleGiftOutsController : ControllerBase
{
    private readonly ISampleGiftOutAppService _appService;

    public SampleGiftOutsController(ISampleGiftOutAppService appService)
    {
        _appService = appService;
    }

    [HttpGet]
    public async Task<ApiResponseDto<PagedResponseDto<SampleGiftOutDto>>> GetList([FromQuery] SampleGiftOutQueryDto query)
        => await _appService.GetPagedListAsync(query);

    [HttpGet("{id:long}")]
    public async Task<ApiResponseDto<SampleGiftOutDto?>> GetById(long id)
        => await _appService.GetByIdAsync(id);

    [HttpPost]
    public async Task<ApiResponseDto<SampleGiftOutDto>> Create([FromBody] SampleGiftOutCreateDto dto)
        => await _appService.CreateAsync(dto);

    [HttpPut("{id:long}")]
    public async Task<ApiResponseDto<SampleGiftOutDto>> Update(long id, [FromBody] SampleGiftOutUpdateDto dto)
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
}
