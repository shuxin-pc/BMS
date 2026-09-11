using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.PosCheckouts;
using Bms.Store.Application.Services;

namespace Bms.Store.Api.Controllers;

/// <summary>
/// POS 快速开单混合结算控制器
/// 将核销（扣卡次+建核销订单）、建单（商品/服务行收款）、开卡（项目卡独立记账）
/// 三类单据在同一数据库事务内执行，任一失败整体回滚，保证结算原子性
/// </summary>
[ApiController]
// 显式写连字符路由：前端请求 /api/store/pos/checkouts
[Route("api/store/pos/checkouts")]
[Authorize]
public class PosCheckoutsController : ControllerBase
{
    private readonly IPosCheckoutAppService _appService;

    public PosCheckoutsController(IPosCheckoutAppService appService)
    {
        _appService = appService;
    }

    /// <summary>
    /// 混合结算（单次调用：核销 + 建单 + 开卡 同一事务）
    /// </summary>
    [HttpPost]
    public async Task<ApiResponseDto<PosCheckoutResultDto>> Checkout([FromBody] PosCheckoutCreateDto dto)
        => await _appService.CheckoutAsync(dto);
}
