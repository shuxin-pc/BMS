using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Technicians;
using Bms.Store.Application.Services;

namespace Bms.Store.Api.Controllers;

/// <summary>
/// 商家技师管理控制器
/// </summary>
[ApiController]
[Route("api/store/[controller]")]
[Authorize]
public class TechniciansController : ControllerBase
{
    private readonly ITechnicianAppService _appService;

    public TechniciansController(ITechnicianAppService appService)
    {
        _appService = appService;
    }

    /// <summary>
    /// 获取商家技师分页列表
    /// </summary>
    [HttpGet]
    public async Task<ApiResponseDto<PagedResponseDto<TechnicianDto>>> GetList([FromQuery] TechnicianQueryDto query)
        => await _appService.GetPagedListAsync(query);

    /// <summary>
    /// 获取商家技师详情
    /// </summary>
    [HttpGet("{id:long}")]
    public async Task<ApiResponseDto<TechnicianDto?>> GetById(long id)
        => await _appService.GetByIdAsync(id);

    /// <summary>
    /// 创建商家技师
    /// </summary>
    [HttpPost]
    public async Task<ApiResponseDto<TechnicianDto>> Create([FromBody] TechnicianCreateDto dto)
        => await _appService.CreateAsync(dto);

    /// <summary>
    /// 更新商家技师
    /// </summary>
    [HttpPut("{id:long}")]
    public async Task<ApiResponseDto<TechnicianDto>> Update(long id, [FromBody] TechnicianUpdateDto dto)
    {
        dto.Id = id;
        return await _appService.UpdateAsync(dto);
    }

    /// <summary>
    /// 删除商家技师
    /// </summary>
    [HttpDelete("{id:long}")]
    public async Task<ApiResponseDto> Delete(long id)
        => await _appService.DeleteAsync(id);

    /// <summary>
    /// 批量删除商家技师
    /// </summary>
    [HttpPost("batch")]
    public async Task<ApiResponseDto> BatchDelete([FromBody] BatchDeleteRequest request)
        => await _appService.BatchDeleteAsync(request.Ids);
}
