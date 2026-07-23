using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Rooms;
using Bms.Store.Application.Services;

namespace Bms.Store.Api.Controllers;

/// <summary>
/// 房间床位管理控制器
/// </summary>
[ApiController]
[Route("api/store/[controller]")]
[Authorize]
public class RoomsController : ControllerBase
{
    private readonly IRoomAppService _appService;

    public RoomsController(IRoomAppService appService)
    {
        _appService = appService;
    }

    [HttpGet]
    public async Task<ApiResponseDto<PagedResponseDto<RoomDto>>> GetList([FromQuery] RoomQueryDto query)
        => await _appService.GetPagedListAsync(query);

    [HttpGet("{id:long}")]
    public async Task<ApiResponseDto<RoomDto?>> GetById(long id)
        => await _appService.GetByIdAsync(id);

    [HttpPost]
    public async Task<ApiResponseDto<RoomDto>> Create([FromBody] RoomCreateDto dto)
        => await _appService.CreateAsync(dto);

    [HttpPut("{id:long}")]
    public async Task<ApiResponseDto<RoomDto>> Update(long id, [FromBody] RoomUpdateDto dto)
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
    /// 根据服务项目查询可用房间/床位列表
    /// 用于预约表单：选择服务项目后拉取符合 RoomType 且时段不冲突的房间
    /// </summary>
    /// <param name="serviceProductId">服务项目商品ID</param>
    /// <param name="startTime">预约开始时间</param>
    /// <param name="endTime">预约结束时间</param>
    /// <param name="excludeAppointmentId">需排除的预约ID（更新场景）</param>
    [HttpGet("available-by-service")]
    public async Task<ApiResponseDto<List<RoomDto>>> GetAvailableByService(
        [FromQuery] long serviceProductId,
        [FromQuery] DateTime startTime,
        [FromQuery] DateTime endTime,
        [FromQuery] long? excludeAppointmentId = null)
        => await _appService.GetAvailableByServiceProductAsync(serviceProductId, startTime, endTime, excludeAppointmentId);
}
