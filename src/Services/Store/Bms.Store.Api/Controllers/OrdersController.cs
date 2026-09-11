using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Orders;
using Bms.Store.Application.Services;

namespace Bms.Store.Api.Controllers;

/// <summary>
/// 订单管理控制器
/// </summary>
[ApiController]
[Route("api/store/[controller]")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IOrderAppService _appService;

    public OrdersController(IOrderAppService appService)
    {
        _appService = appService;
    }

    /// <summary>
    /// 获取订单分页列表
    /// </summary>
    [HttpGet]
    public async Task<ApiResponseDto<PagedResponseDto<OrderDto>>> GetList([FromQuery] OrderQueryDto query)
        => await _appService.GetPagedListAsync(query);

    /// <summary>
    /// 获取今日待补录订单列表（为站内信功能预留数据源）
    /// </summary>
    [HttpGet("today-backfill")]
    public async Task<ApiResponseDto<List<OrderDto>>> GetTodayBackfill()
        => await _appService.GetTodayBackfillOrdersAsync();

    /// <summary>
    /// 获取订单详情
    /// </summary>
    [HttpGet("{id:long}")]
    public async Task<ApiResponseDto<OrderDto?>> GetById(long id)
        => await _appService.GetByIdAsync(id);

    /// <summary>
    /// 创建订单
    /// </summary>
    [HttpPost]
    public async Task<ApiResponseDto<OrderDto>> Create([FromBody] OrderCreateDto dto)
        => await _appService.CreateAsync(dto);

    /// <summary>
    /// 订单退款（事务包裹，按订单类型联动库存/项目卡/储值/积分）
    /// </summary>
    /// <param name="id">订单ID</param>
    /// <param name="dto">退款请求</param>
    /// <returns>退款结果</returns>
    [HttpPost("{id:long}/refund")]
    public async Task<ApiResponseDto<RefundResultDto>> Refund(long id, [FromBody] RefundRequestDto dto)
    {
        dto.OrderId = id;
        return await _appService.RefundAsync(dto);
    }

    /// <summary>
    /// 取消订单（事务包裹，按 OrderType 全量回滚库存/BOM/项目卡/积分/储值/统计/消费记录）
    /// 订单 Status 改为 4（已取消），视为订单未发生
    /// </summary>
    /// <param name="id">订单ID</param>
    /// <param name="dto">取消请求（含取消原因）</param>
    [HttpPost("{id:long}/cancel")]
    public async Task<ApiResponseDto> Cancel(long id, [FromBody] OrderCancelDto dto)
        => await _appService.CancelAsync(id, dto);
}
