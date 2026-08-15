using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.EquipmentTypes;
using Bms.Store.Application.Services;

namespace Bms.Store.Api.Controllers;

/// <summary>
/// 设备类型管理控制器（租户级共享数据）
/// </summary>
[ApiController]
[Route("api/store/[controller]")]
[Authorize]
public class EquipmentTypesController : ControllerBase
{
    private readonly IEquipmentTypeAppService _appService;

    public EquipmentTypesController(IEquipmentTypeAppService appService)
    {
        _appService = appService;
    }

    [HttpGet]
    public async Task<ApiResponseDto<PagedResponseDto<EquipmentTypeDto>>> GetList([FromQuery] EquipmentTypeQueryDto query)
        => await _appService.GetPagedListAsync(query);

    /// <summary>
    /// 获取全部启用设备类型（用于下拉选择）
    /// </summary>
    [HttpGet("options")]
    public async Task<ApiResponseDto<List<EquipmentTypeDto>>> GetOptions()
        => await _appService.GetAllAsync();

    [HttpGet("{id:long}")]
    public async Task<ApiResponseDto<EquipmentTypeDto?>> GetById(long id)
        => await _appService.GetByIdAsync(id);

    [HttpPost]
    public async Task<ApiResponseDto<EquipmentTypeDto>> Create([FromBody] EquipmentTypeCreateDto dto)
        => await _appService.CreateAsync(dto);

    [HttpPut("{id:long}")]
    public async Task<ApiResponseDto<EquipmentTypeDto>> Update(long id, [FromBody] EquipmentTypeUpdateDto dto)
    {
        dto.Id = id;
        return await _appService.UpdateAsync(dto);
    }

    [HttpDelete("{id:long}")]
    public async Task<ApiResponseDto> Delete(long id)
        => await _appService.DeleteAsync(id);
}
