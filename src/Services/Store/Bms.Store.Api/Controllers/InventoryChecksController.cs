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
    /// 获取盘点专用商品选项（含账面库存和成本价，用于新增盘点下拉选择）
    /// </summary>
    [HttpGet("product-options")]
    public async Task<ApiResponseDto<List<InventoryCheckProductOptionDto>>> GetProductOptions()
        => await _appService.GetProductOptionsForCheckAsync();

    /// <summary>
    /// 盘盈批次号查询：校验批次号在当前商品/门店的存在性，找到返回批次详情（含生产日期/保质期/过期日期），找不到返回 null
    /// </summary>
    [HttpGet("batch-lookup")]
    public async Task<ApiResponseDto<InventoryCheckBatchLookupDto?>> GetBatchLookup([FromQuery] long productId, [FromQuery] string batchNo)
        => await _appService.GetBatchLookupForCheckAsync(productId, batchNo);

    /// <summary>
    /// 查询当日该商品是否已有非取消状态的盘点记录（用于前端软约束提示）
    /// </summary>
    [HttpGet("today-check")]
    public async Task<ApiResponseDto<bool>> HasProductCheckedToday([FromQuery] long productId)
        => await _appService.HasProductCheckedTodayAsync(productId);

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
    /// 创建并提交盘点单（原子操作）：事务内完成创建+提交，不产生草稿残留
    /// </summary>
    [HttpPost("create-and-submit")]
    public async Task<ApiResponseDto<InventoryCheckDto>> CreateAndSubmit([FromBody] CreateAndSubmitCheckDto dto)
        => await _appService.CreateAndSubmitAsync(dto);

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
