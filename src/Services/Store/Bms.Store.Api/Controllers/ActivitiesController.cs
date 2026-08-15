using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Activities;
using Bms.Store.Application.Services;

namespace Bms.Store.Api.Controllers;

/// <summary>
/// 活动管理控制器
/// </summary>
[ApiController]
[Route("api/store/[controller]")]
[Authorize]
public class ActivitiesController : ControllerBase
{
    private readonly IActivityAppService _appService;

    public ActivitiesController(IActivityAppService appService)
    {
        _appService = appService;
    }

    /// <summary>
    /// 获取活动分页列表
    /// </summary>
    [HttpGet]
    public async Task<ApiResponseDto<PagedResponseDto<ActivityDto>>> GetList([FromQuery] ActivityQueryDto query)
        => await _appService.GetPagedListAsync(query);

    /// <summary>
    /// 获取活动详情
    /// </summary>
    [HttpGet("{id:long}")]
    public async Task<ApiResponseDto<ActivityDto?>> GetById(long id)
        => await _appService.GetByIdAsync(id);

    /// <summary>
    /// 创建活动
    /// </summary>
    [HttpPost]
    public async Task<ApiResponseDto<ActivityDto>> Create([FromBody] ActivityCreateDto dto)
        => await _appService.CreateAsync(dto);

    /// <summary>
    /// 更新活动
    /// </summary>
    [HttpPut("{id:long}")]
    public async Task<ApiResponseDto<ActivityDto>> Update(long id, [FromBody] ActivityUpdateDto dto)
    {
        dto.Id = id;
        return await _appService.UpdateAsync(dto);
    }

    /// <summary>
    /// 删除活动（软删除）
    /// </summary>
    [HttpDelete("{id:long}")]
    public async Task<ApiResponseDto> Delete(long id)
        => await _appService.DeleteAsync(id);

    /// <summary>
    /// 获取进行中活动下拉选项（供其他业务关联选择）
    /// </summary>
    [HttpGet("options")]
    public async Task<ApiResponseDto<List<ActivityOptionDto>>> GetOptions()
        => await _appService.GetOptionsAsync();
}
