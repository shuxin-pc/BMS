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

    /// <summary>
    /// 按服务项目查询可用技师（预约时过滤技师下拉 + 服务项目页展示可服务技师）
    /// serviceProductId/masterId 均为空时返回指定来源全部启用技师；
    /// masterId（商品主档ID）自动反查租户内 ServiceProduct 后按技能匹配。
    /// </summary>
    [HttpGet("available-by-service")]
    public async Task<ApiResponseDto<List<TechnicianDto>>> GetAvailableByService(
        [FromQuery] long? serviceProductId,
        [FromQuery] long? masterId,
        [FromQuery] int? source)
        => await _appService.GetAvailableByServiceAsync(serviceProductId, masterId, source);

    /// <summary>
    /// 查询技师可服务的服务项目列表（技师页展示擅长项目）
    /// </summary>
    [HttpGet("{id:long}/services")]
    public async Task<ApiResponseDto<List<TechnicianServiceItemDto>>> GetServices(long id)
        => await _appService.GetServiceProductsByTechnicianAsync(id);
}
