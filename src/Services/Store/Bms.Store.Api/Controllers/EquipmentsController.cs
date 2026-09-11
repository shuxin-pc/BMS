using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Equipments;
using Bms.Store.Application.Services;

namespace Bms.Store.Api.Controllers;

/// <summary>
/// 设备台账管理控制器
/// </summary>
[ApiController]
[Route("api/store/[controller]")]
[Authorize]
public class EquipmentsController : ControllerBase
{
    private readonly IEquipmentAppService _appService;

    public EquipmentsController(IEquipmentAppService appService)
    {
        _appService = appService;
    }

    [HttpGet]
    public async Task<ApiResponseDto<PagedResponseDto<EquipmentDto>>> GetList([FromQuery] EquipmentQueryDto query)
        => await _appService.GetPagedListAsync(query);

    /// <summary>
    /// 查询即将到期保养的设备列表（默认未来 7 天，含已过期未保养）
    /// </summary>
    /// <param name="days">未来天数，默认 7</param>
    [HttpGet("upcoming-maintenance")]
    public async Task<ApiResponseDto<List<EquipmentDto>>> GetUpcomingMaintenance([FromQuery] int days = 7)
        => await _appService.GetUpcomingMaintenanceAsync(days);

    /// <summary>
    /// 根据服务项目查询可用设备列表
    /// 用于预约表单：选择服务项目后拉取符合设备类型且时段不冲突的设备
    /// </summary>
    /// <param name="serviceProductId">服务项目子表ID（预约页语境）</param>
    /// <param name="masterId">商品主档ID（服务项目页语境，自动反查租户内 ServiceProduct）</param>
    /// <param name="startTime">预约开始时间；不传时仅按服务项目设备类型过滤，不排除冲突设备</param>
    /// <param name="endTime">预约结束时间；不传时仅按服务项目设备类型过滤，不排除冲突设备</param>
    /// <param name="excludeAppointmentId">需排除的预约ID（更新场景）</param>
    [HttpGet("available-by-service")]
    public async Task<ApiResponseDto<List<EquipmentDto>>> GetAvailableByService(
        [FromQuery] long? serviceProductId,
        [FromQuery] long? masterId,
        [FromQuery] DateTime? startTime,
        [FromQuery] DateTime? endTime,
        [FromQuery] long? excludeAppointmentId = null)
        => await _appService.GetAvailableByServiceProductAsync(serviceProductId, masterId, startTime, endTime, excludeAppointmentId);

    [HttpGet("{id:long}")]
    public async Task<ApiResponseDto<EquipmentDto?>> GetById(long id)
        => await _appService.GetByIdAsync(id);

    [HttpPost]
    public async Task<ApiResponseDto<EquipmentDto>> Create([FromBody] EquipmentCreateDto dto)
        => await _appService.CreateAsync(dto);

    [HttpPut("{id:long}")]
    public async Task<ApiResponseDto<EquipmentDto>> Update(long id, [FromBody] EquipmentUpdateDto dto)
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
