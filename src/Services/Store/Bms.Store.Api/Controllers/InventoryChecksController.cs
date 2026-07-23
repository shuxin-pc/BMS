using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Inventories;
using Bms.Store.Application.Services;

namespace Bms.Store.Api.Controllers;

/// <summary>
/// 库存盘点记录管理控制器
/// </summary>
[ApiController]
[Route("api/store/[controller]")]
[Authorize]
public class InventoryChecksController : ControllerBase
{
    private readonly IInventoryCheckAppService _appService;

    public InventoryChecksController(IInventoryCheckAppService appService)
    {
        _appService = appService;
    }

    /// <summary>
    /// 获取库存盘点记录分页列表
    /// </summary>
    [HttpGet]
    public async Task<ApiResponseDto<PagedResponseDto<InventoryCheckDto>>> GetList([FromQuery] InventoryCheckQueryDto query)
        => await _appService.GetPagedListAsync(query);

    /// <summary>
    /// 获取库存盘点记录详情
    /// </summary>
    [HttpGet("{id:long}")]
    public async Task<ApiResponseDto<InventoryCheckDto?>> GetById(long id)
        => await _appService.GetByIdAsync(id);

    /// <summary>
    /// 创建库存盘点记录
    /// </summary>
    [HttpPost]
    public async Task<ApiResponseDto<InventoryCheckDto>> Create([FromBody] InventoryCheckCreateDto dto)
        => await _appService.CreateAsync(dto);

    /// <summary>
    /// 更新库存盘点记录
    /// </summary>
    [HttpPut("{id:long}")]
    public async Task<ApiResponseDto<InventoryCheckDto>> Update(long id, [FromBody] InventoryCheckUpdateDto dto)
    {
        dto.Id = id;
        return await _appService.UpdateAsync(dto);
    }

    /// <summary>
    /// 删除库存盘点记录
    /// </summary>
    [HttpDelete("{id:long}")]
    public async Task<ApiResponseDto> Delete(long id)
        => await _appService.DeleteAsync(id);

    /// <summary>
    /// 批量删除库存盘点记录
    /// </summary>
    [HttpPost("batch")]
    public async Task<ApiResponseDto> BatchDelete([FromBody] BatchDeleteRequest request)
        => await _appService.BatchDeleteAsync(request.Ids);

    /// <summary>
    /// 提交盘点单：录入实际数量，计算差异，自动调整库存并写入流水，状态转已完成
    /// </summary>
    [HttpPost("{id:long}/submit")]
    public async Task<ApiResponseDto<InventoryCheckDto>> Submit(long id, [FromBody] SubmitCheckDto dto)
    {
        dto.Id = id;
        return await _appService.SubmitCheckAsync(dto);
    }

    /// <summary>
    /// 取消盘点单：草稿转已取消（已完成的不可取消）
    /// </summary>
    [HttpPost("{id:long}/cancel")]
    public async Task<ApiResponseDto> Cancel(long id)
        => await _appService.CancelAsync(id);
}
