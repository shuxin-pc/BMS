namespace Bms.Store.Application.Dtos.PosCheckouts;

/// <summary>
/// POS 快速开单混合结算结果 DTO
/// 三类单据全部成功后返回，供前端聚合展示本次交易
/// </summary>
public class PosCheckoutResultDto
{
    /// <summary>
    /// 购物车结算批次号（与请求一致，用于前端跨单据聚合追溯）
    /// </summary>
    public string CheckoutSessionNo { get; set; } = string.Empty;

    /// <summary>
    /// 商品/服务订单产出（仅购物车含商品/服务行时返回）
    /// </summary>
    public List<PosCheckoutOrderResultDto> Orders { get; set; } = new();

    /// <summary>
    /// 项目卡核销产出（仅购物车含核销行时返回）
    /// </summary>
    public List<PosCheckoutVerifyResultDto> Verifies { get; set; } = new();

    /// <summary>
    /// 项目卡开卡产出（仅购物车含开卡行时返回）
    /// </summary>
    public List<PosCheckoutSaleResultDto> Sales { get; set; } = new();
}

/// <summary>
/// 商品/服务订单结果项
/// </summary>
public class PosCheckoutOrderResultDto
{
    /// <summary>
    /// 订单ID
    /// </summary>
    public long OrderId { get; set; }

    /// <summary>
    /// 订单号
    /// </summary>
    public string OrderNo { get; set; } = string.Empty;

    /// <summary>
    /// 实付金额（仅商品/服务行的收款金额，不含开卡费）
    /// </summary>
    public decimal Amount { get; set; }
}

/// <summary>
/// 项目卡核销结果项
/// </summary>
public class PosCheckoutVerifyResultDto
{
    /// <summary>
    /// 项目卡销售记录ID（前端据此映射卡名展示）
    /// </summary>
    public long CardSaleId { get; set; }

    /// <summary>
    /// 本次核销总次数
    /// </summary>
    public int Times { get; set; }

    /// <summary>
    /// 本次核销金额
    /// </summary>
    public decimal Amount { get; set; }
}

/// <summary>
/// 项目卡开卡结果项
/// </summary>
public class PosCheckoutSaleResultDto
{
    /// <summary>
    /// 销售记录ID
    /// </summary>
    public long SaleId { get; set; }

    /// <summary>
    /// 开卡售价（线下独立收款金额）
    /// </summary>
    public decimal Amount { get; set; }
}
