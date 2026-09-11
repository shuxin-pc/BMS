using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.PosCheckouts;

namespace Bms.Store.Application.Services;

/// <summary>
/// POS 快速开单混合结算应用服务接口
/// 将核销（扣卡次+建核销订单）、建单（商品/服务行收款）、开卡（项目卡独立记账）
/// 三类单据在同一数据库事务内执行，任一失败整体回滚，保证结算原子性
/// </summary>
public interface IPosCheckoutAppService
{
    /// <summary>
    /// 混合结算：按 核销 → 建单 → 开卡 顺序在同一事务内执行三类单据
    /// 任一单据失败则回滚全部已成功单据，返回对应的错误消息与状态码
    /// </summary>
    Task<ApiResponseDto<PosCheckoutResultDto>> CheckoutAsync(PosCheckoutCreateDto dto);
}
