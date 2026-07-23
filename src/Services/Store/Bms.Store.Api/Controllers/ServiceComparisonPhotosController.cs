using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Customers;
using Bms.Store.Application.Services;

namespace Bms.Store.Api.Controllers;

[ApiController]
[Route("api/store/[controller]")]
[Authorize]
public class ServiceComparisonPhotosController : ControllerBase
{
    private readonly IServiceComparisonPhotoAppService _appService;

    public ServiceComparisonPhotosController(IServiceComparisonPhotoAppService appService)
    {
        _appService = appService;
    }

    [HttpGet]
    public async Task<ApiResponseDto<PagedResponseDto<ServiceComparisonPhotoDto>>> GetList([FromQuery] ServiceComparisonPhotoQueryDto query)
        => await _appService.GetPagedListAsync(query);

    [HttpGet("{id:long}")]
    public async Task<ApiResponseDto<ServiceComparisonPhotoDto?>> GetById(long id)
        => await _appService.GetByIdAsync(id);

    [HttpPost]
    public async Task<ApiResponseDto<ServiceComparisonPhotoDto>> Create([FromBody] ServiceComparisonPhotoCreateDto dto)
        => await _appService.CreateAsync(dto);

    [HttpPut("{id:long}")]
    public async Task<ApiResponseDto<ServiceComparisonPhotoDto>> Update(long id, [FromBody] ServiceComparisonPhotoUpdateDto dto)
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
