using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Inventories;
using Bms.Store.Application.Services;

namespace Bms.Store.Api.Controllers;

/// <summary>
/// 库存预警管理控制器
/// </summary>
[ApiController]
[Route("api/store/[controller]")]
[Authorize]
public class InventoryAlertsController : ControllerBase
{
    private readonly IInventoryAlertAppService _appService;

    public InventoryAlertsController(IInventoryAlertAppService appService)
    {
        _appService = appService;
    }

    /// <summary>
    /// 获取库存预警分页列表
    /// </summary>
    [HttpGet]
    public async Task<ApiResponseDto<PagedResponseDto<InventoryAlertDto>>> GetList([FromQuery] InventoryAlertQueryDto query)
        => await _appService.GetPagedListAsync(query);

    /// <summary>
    /// 获取库存预警详情
    /// </summary>
    [HttpGet("{id:long}")]
    public async Task<ApiResponseDto<InventoryAlertDto?>> GetById(long id)
        => await _appService.GetByIdAsync(id);

    /// <summary>
    /// 创建库存预警
    /// </summary>
    [HttpPost]
    public async Task<ApiResponseDto<InventoryAlertDto>> Create([FromBody] InventoryAlertCreateDto dto)
        => await _appService.CreateAsync(dto);

    /// <summary>
    /// 更新库存预警
    /// </summary>
    [HttpPut("{id:long}")]
    public async Task<ApiResponseDto<InventoryAlertDto>> Update(long id, [FromBody] InventoryAlertUpdateDto dto)
    {
        dto.Id = id;
        return await _appService.UpdateAsync(dto);
    }

    /// <summary>
    /// 删除库存预警
    /// </summary>
    [HttpDelete("{id:long}")]
    public async Task<ApiResponseDto> Delete(long id)
        => await _appService.DeleteAsync(id);

    /// <summary>
    /// 批量删除库存预警
    /// </summary>
    [HttpPost("batch")]
    public async Task<ApiResponseDto> BatchDelete([FromBody] BatchDeleteRequest request)
        => await _appService.BatchDeleteAsync(request.Ids);

    /// <summary>
    /// 手动触发预警扫描（低库存/效期/积压）
    /// </summary>
    [HttpPost("scan")]
    public async Task<ApiResponseDto<InventoryAlertScanResultDto>> Scan()
        => await _appService.ScanAsync();

    /// <summary>
    /// 标记预警已处理
    /// </summary>
    [HttpPost("{id:long}/process")]
    public async Task<ApiResponseDto> Process(long id, [FromBody] ProcessAlertRequest? request)
        => await _appService.ProcessAsync(id, request?.Remark);
}

/// <summary>
/// 预警处理请求
/// </summary>
public class ProcessAlertRequest
{
    /// <summary>处理备注</summary>
    public string? Remark { get; set; }
}
