using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.TreatmentCards;
using Bms.Store.Application.Services;

namespace Bms.Store.Api.Controllers;

/// <summary>
/// 项目卡项目关联管理控制器
/// </summary>
[ApiController]
[Route("api/store/[controller]")]
[Authorize]
public class CourseCardItemsController : ControllerBase
{
    private readonly ICourseCardItemAppService _appService;

    public CourseCardItemsController(ICourseCardItemAppService appService)
    {
        _appService = appService;
    }

    /// <summary>
    /// 获取项目卡项目关联分页列表
    /// </summary>
    [HttpGet]
    public async Task<ApiResponseDto<PagedResponseDto<CourseCardItemDto>>> GetList([FromQuery] CourseCardItemQueryDto query)
        => await _appService.GetPagedListAsync(query);

    /// <summary>
    /// 获取项目卡项目关联详情
    /// </summary>
    [HttpGet("{id:long}")]
    public async Task<ApiResponseDto<CourseCardItemDto?>> GetById(long id)
        => await _appService.GetByIdAsync(id);

    /// <summary>
    /// 创建项目卡项目关联
    /// </summary>
    [HttpPost]
    public async Task<ApiResponseDto<CourseCardItemDto>> Create([FromBody] CourseCardItemCreateDto dto)
        => await _appService.CreateAsync(dto);

    /// <summary>
    /// 更新项目卡项目关联
    /// </summary>
    [HttpPut("{id:long}")]
    public async Task<ApiResponseDto<CourseCardItemDto>> Update(long id, [FromBody] CourseCardItemUpdateDto dto)
    {
        dto.Id = id;
        return await _appService.UpdateAsync(dto);
    }

    /// <summary>
    /// 删除项目卡项目关联
    /// </summary>
    [HttpDelete("{id:long}")]
    public async Task<ApiResponseDto> Delete(long id)
        => await _appService.DeleteAsync(id);

    /// <summary>
    /// 批量删除项目卡项目关联
    /// </summary>
    [HttpPost("batch")]
    public async Task<ApiResponseDto> BatchDelete([FromBody] BatchDeleteRequest request)
        => await _appService.BatchDeleteAsync(request.Ids);
}
