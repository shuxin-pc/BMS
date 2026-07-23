using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Inventories;
using Bms.Store.Application.Services;

namespace Bms.Store.Api.Controllers;

/// <summary>
/// 库存流水管理控制器
/// </summary>
[ApiController]
[Route("api/store/[controller]")]
[Authorize]
public class InventoryLogsController : ControllerBase
{
    private readonly IInventoryLogAppService _appService;

    public InventoryLogsController(IInventoryLogAppService appService)
    {
        _appService = appService;
    }

    /// <summary>
    /// 获取库存流水分页列表
    /// </summary>
    [HttpGet]
    public async Task<ApiResponseDto<PagedResponseDto<InventoryLogDto>>> GetList([FromQuery] InventoryLogQueryDto query)
        => await _appService.GetPagedListAsync(query);

    /// <summary>
    /// 获取库存流水详情
    /// </summary>
    [HttpGet("{id:long}")]
    public async Task<ApiResponseDto<InventoryLogDto?>> GetById(long id)
        => await _appService.GetByIdAsync(id);

    /// <summary>
    /// 创建库存流水
    /// </summary>
    [HttpPost]
    public async Task<ApiResponseDto<InventoryLogDto>> Create([FromBody] InventoryLogCreateDto dto)
        => await _appService.CreateAsync(dto);

    /// <summary>
    /// 更新库存流水
    /// </summary>
    [HttpPut("{id:long}")]
    public async Task<ApiResponseDto<InventoryLogDto>> Update(long id, [FromBody] InventoryLogUpdateDto dto)
    {
        dto.Id = id;
        return await _appService.UpdateAsync(dto);
    }

    /// <summary>
    /// 删除库存流水
    /// </summary>
    [HttpDelete("{id:long}")]
    public async Task<ApiResponseDto> Delete(long id)
        => await _appService.DeleteAsync(id);

    /// <summary>
    /// 批量删除库存流水
    /// </summary>
    [HttpPost("batch")]
    public async Task<ApiResponseDto> BatchDelete([FromBody] BatchDeleteRequest request)
        => await _appService.BatchDeleteAsync(request.Ids);
}
