using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.PointsRules;
using Bms.Store.Application.Services;

namespace Bms.Store.Api.Controllers;

/// <summary>
/// 积分规则管理控制器
/// </summary>
[ApiController]
[Route("api/store/[controller]")]
[Authorize]
public class PointsRulesController : ControllerBase
{
    private readonly IPointsRuleAppService _appService;

    public PointsRulesController(IPointsRuleAppService appService)
    {
        _appService = appService;
    }

    /// <summary>
    /// 获取积分规则分页列表
    /// </summary>
    [HttpGet]
    public async Task<ApiResponseDto<PagedResponseDto<PointsRuleDto>>> GetList([FromQuery] PointsRuleQueryDto query)
        => await _appService.GetPagedListAsync(query);

    /// <summary>
    /// 获取积分规则详情
    /// </summary>
    [HttpGet("{id:long}")]
    public async Task<ApiResponseDto<PointsRuleDto?>> GetById(long id)
        => await _appService.GetByIdAsync(id);

    /// <summary>
    /// 创建积分规则
    /// </summary>
    [HttpPost]
    public async Task<ApiResponseDto<PointsRuleDto>> Create([FromBody] PointsRuleCreateDto dto)
        => await _appService.CreateAsync(dto);

    /// <summary>
    /// 更新积分规则
    /// </summary>
    [HttpPut("{id:long}")]
    public async Task<ApiResponseDto<PointsRuleDto>> Update(long id, [FromBody] PointsRuleUpdateDto dto)
    {
        dto.Id = id;
        return await _appService.UpdateAsync(dto);
    }

    /// <summary>
    /// 删除积分规则
    /// </summary>
    [HttpDelete("{id:long}")]
    public async Task<ApiResponseDto> Delete(long id)
        => await _appService.DeleteAsync(id);

    /// <summary>
    /// 批量删除积分规则
    /// </summary>
    [HttpPost("batch")]
    public async Task<ApiResponseDto> BatchDelete([FromBody] BatchDeleteRequest request)
        => await _appService.BatchDeleteAsync(request.Ids);
}
