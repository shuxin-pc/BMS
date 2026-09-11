using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.ParkedOrders;
using Bms.Store.Application.Services;

namespace Bms.Store.Api.Controllers;

/// <summary>
/// POS 挂单控制器
/// 快速开单页挂单/取单/取消操作，门店内共享
/// </summary>
[ApiController]
// 注意：必须显式写连字符路由。前端请求 /api/store/parked-orders（带连字符），
// 而 [controller] 占位符生成 /api/store/ParkedOrders（无连字符）无法匹配，会 404
[Route("api/store/parked-orders")]
[Authorize]
public class ParkedOrdersController : ControllerBase
{
    private readonly IParkedOrderAppService _appService;

    public ParkedOrdersController(IParkedOrderAppService appService)
    {
        _appService = appService;
    }

    /// <summary>
    /// 挂单（保存当前购物车草稿）
    /// </summary>
    [HttpPost]
    public async Task<ApiResponseDto<ParkedOrderDto>> Create([FromBody] ParkedOrderCreateDto dto)
        => await _appService.CreateAsync(dto);

    /// <summary>
    /// 挂单分页列表（挂起状态）
    /// </summary>
    [HttpGet]
    public async Task<ApiResponseDto<PagedResponseDto<ParkedOrderDto>>> GetList([FromQuery] ParkedOrderQueryDto query)
        => await _appService.GetPagedListAsync(query);

    /// <summary>
    /// 取单（恢复挂单购物车，挂单置为已取走）
    /// </summary>
    [HttpPost("{id:long}/resume")]
    public async Task<ApiResponseDto<ParkedOrderDto>> Resume(long id)
        => await _appService.ResumeAsync(id);

    /// <summary>
    /// 取消挂单
    /// </summary>
    [HttpPost("{id:long}/cancel")]
    public async Task<ApiResponseDto> Cancel(long id)
        => await _appService.CancelAsync(id);
}
