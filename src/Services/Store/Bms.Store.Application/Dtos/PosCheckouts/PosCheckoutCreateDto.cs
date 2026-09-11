using Bms.Store.Application.Dtos.Orders;
using Bms.Store.Application.Dtos.TreatmentCards;

namespace Bms.Store.Application.Dtos.PosCheckouts;

/// <summary>
/// POS 快速开单混合结算请求 DTO
/// 将核销（扣卡次+建核销订单）、建单（商品/服务行收款）、开卡（项目卡独立记账）
/// 三类单据合并为一次请求，由 PosCheckoutAppService 在同一数据库事务内执行，
/// 任一单据失败整体回滚，保证结算原子性（见 D2 结算时序）
/// </summary>
public class PosCheckoutCreateDto
{
    /// <summary>
    /// 购物车结算批次号（订单/核销/开卡三单据共用，用于跨单据聚合追溯）
    /// </summary>
    public string CheckoutSessionNo { get; set; } = string.Empty;

    /// <summary>
    /// 商品/服务行建单（可空：纯核销/纯开卡购物车不传）
    /// 复用 OrderCreateDto，OrderType 由前端按购物车是否含服务项目决定（2=含服务，1=纯零售）
    /// </summary>
    public OrderCreateDto? Order { get; set; }

    /// <summary>
    /// 项目卡核销组（可空：无核销行不传）
    /// 同一张卡多次加入购物车时由前端按 CardSaleId 聚合为一项
    /// </summary>
    public List<PosCheckoutVerifyItemDto>? Verifies { get; set; }

    /// <summary>
    /// 项目卡开卡组（可空：无开卡行不传）
    /// 复用 TreatmentCardSaleCreateDto，金额为线下独立收款，不进系统支付/日结
    /// </summary>
    public List<TreatmentCardSaleCreateDto>? Sales { get; set; }
}

/// <summary>
/// 混合结算核销子项（购物车核销行按 CardSaleId 聚合后一次核销）
/// </summary>
public class PosCheckoutVerifyItemDto
{
    /// <summary>
    /// 项目卡销售记录ID
    /// </summary>
    public long CardSaleId { get; set; }

    /// <summary>
    /// 核销项目列表（每项含项目ID/次数/技师/房间/设备/服务时间）
    /// </summary>
    public List<TreatmentCardVerifyItemInput> Items { get; set; } = new();
}
