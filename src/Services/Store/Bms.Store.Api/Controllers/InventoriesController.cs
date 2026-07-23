using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Inventories;
using Bms.Store.Application.Services;

namespace Bms.Store.Api.Controllers;

/// <summary>
/// 库存管理控制器
/// </summary>
[ApiController]
[Route("api/store/[controller]")]
[Authorize]
public class InventoriesController : ControllerBase
{
    private readonly IInventoryAppService _appService;

    public InventoriesController(IInventoryAppService appService)
    {
        _appService = appService;
    }

    /// <summary>
    /// 获取库存分页列表
    /// </summary>
    [HttpGet]
    public async Task<ApiResponseDto<PagedResponseDto<InventoryDto>>> GetList([FromQuery] InventoryQueryDto query)
        => await _appService.GetPagedListAsync(query);

    /// <summary>
    /// 获取库存详情
    /// </summary>
    [HttpGet("{id:long}")]
    public async Task<ApiResponseDto<InventoryDto?>> GetById(long id)
        => await _appService.GetByIdAsync(id);

    /// <summary>
    /// 创建库存
    /// </summary>
    [HttpPost]
    public async Task<ApiResponseDto<InventoryDto>> Create([FromBody] InventoryCreateDto dto)
        => await _appService.CreateAsync(dto);

    /// <summary>
    /// 更新库存
    /// </summary>
    [HttpPut("{id:long}")]
    public async Task<ApiResponseDto<InventoryDto>> Update(long id, [FromBody] InventoryUpdateDto dto)
    {
        dto.Id = id;
        return await _appService.UpdateAsync(dto);
    }

    /// <summary>
    /// 删除库存
    /// </summary>
    [HttpDelete("{id:long}")]
    public async Task<ApiResponseDto> Delete(long id)
        => await _appService.DeleteAsync(id);

    /// <summary>
    /// 批量删除库存
    /// </summary>
    [HttpPost("batch")]
    public async Task<ApiResponseDto> BatchDelete([FromBody] BatchDeleteRequest request)
        => await _appService.BatchDeleteAsync(request.Ids);
}
