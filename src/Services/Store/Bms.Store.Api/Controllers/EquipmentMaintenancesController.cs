using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Equipments;
using Bms.Store.Application.Services;

namespace Bms.Store.Api.Controllers;

/// <summary>
/// 设备维护记录管理控制器
/// </summary>
[ApiController]
[Route("api/store/equipment-maintenances")]
[Authorize]
public class EquipmentMaintenancesController : ControllerBase
{
    private readonly IEquipmentMaintenanceAppService _appService;

    public EquipmentMaintenancesController(IEquipmentMaintenanceAppService appService)
    {
        _appService = appService;
    }

    /// <summary>
    /// 获取设备维护记录分页列表
    /// </summary>
    [HttpGet]
    public async Task<ApiResponseDto<PagedResponseDto<EquipmentMaintenanceDto>>> GetList([FromQuery] EquipmentMaintenanceQueryDto query)
        => await _appService.GetPagedListAsync(query);

    /// <summary>
    /// 获取设备维护记录详情
    /// </summary>
    [HttpGet("{id:long}")]
    public async Task<ApiResponseDto<EquipmentMaintenanceDto?>> GetById(long id)
        => await _appService.GetByIdAsync(id);

    /// <summary>
    /// 创建设备维护记录
    /// </summary>
    [HttpPost]
    public async Task<ApiResponseDto<EquipmentMaintenanceDto>> Create([FromBody] EquipmentMaintenanceCreateDto dto)
        => await _appService.CreateAsync(dto);

    /// <summary>
    /// 更新设备维护记录
    /// </summary>
    [HttpPut("{id:long}")]
    public async Task<ApiResponseDto<EquipmentMaintenanceDto>> Update(long id, [FromBody] EquipmentMaintenanceUpdateDto dto)
    {
        dto.Id = id;
        return await _appService.UpdateAsync(dto);
    }

    /// <summary>
    /// 删除设备维护记录
    /// </summary>
    [HttpDelete("{id:long}")]
    public async Task<ApiResponseDto> Delete(long id)
        => await _appService.DeleteAsync(id);

    /// <summary>
    /// 批量删除设备维护记录
    /// </summary>
    [HttpPost("batch")]
    public async Task<ApiResponseDto> BatchDelete([FromBody] BatchDeleteRequest request)
        => await _appService.BatchDeleteAsync(request.Ids);
}
