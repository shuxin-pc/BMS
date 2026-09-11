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
// 使用显式 kebab-case 路由，与前端 /api/store/equipment-types 及项目内多词控制器约定保持一致
// （参照 EquipmentMaintenancesController、DailySettlementsController）
[Route("api/store/equipment-types")]
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
    /// 获取全部启用设备类型（用于下拉选择，仅叶子节点即具体型号）
    /// </summary>
    [HttpGet("options")]
    public async Task<ApiResponseDto<List<EquipmentTypeDto>>> GetOptions()
        => await _appService.GetAllAsync();

    /// <summary>
    /// 获取设备类型树（含父级分类节点，用于管理页树形展示）
    /// </summary>
    [HttpGet("tree")]
    public async Task<ApiResponseDto<List<EquipmentTypeDto>>> GetTree()
        => await _appService.GetTreeAsync();

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
