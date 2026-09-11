namespace Bms.Store.Application.Dtos.TreatmentCards;

/// <summary>
/// 创建项目卡销售记录请求 DTO
/// </summary>
public class TreatmentCardSaleCreateDto
{
    /// <summary>
    /// 购买门店ID（可空，项目卡在租户内跨店通用）
    /// </summary>
    public long? StoreId { get; set; }

    /// <summary>
    /// 购买门店编码
    /// </summary>
    public string? StoreCode { get; set; }

    /// <summary>
    /// 项目卡ID
    /// </summary>
    public long CardId { get; set; }

    /// <summary>
    /// 客户ID
    /// </summary>
    public long CustomerId { get; set; }

    /// <summary>
    /// 购买日期
    /// </summary>
    public DateTime PurchaseDate { get; set; }

    /// <summary>
    /// 购买金额
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// 状态（1:有效 2:已用完 3:已过期）
    /// </summary>
    public int Status { get; set; } = 1;

    /// <summary>
    /// 项目卡包含的项目列表（前端传入商品、次数、原价；折算单价由后端按实际购买金额分摊计算并锁定）
    /// </summary>
    public List<TreatmentCardSaleItemCreateDto> Items { get; set; } = new();

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 购物车结算批次号（POS 购物车一次结算生成，用于订单/核销/开卡三单据聚合追溯）
    /// 非购物车结算（独立开卡）不传
    /// </summary>
    public string? CheckoutSessionNo { get; set; }

    /// <summary>
    /// 支付方式（1:现金 2:支付宝 3:微信 4:银行卡 5:储值卡 6:积分抵扣 7:组合支付）
    /// POS 统一支付方式：开卡与同批次订单支付方式一致，由前端传入（可空=历史兼容）
    /// </summary>
    public int? PayMethod { get; set; }

    /// <summary>
    /// 组合支付-类别1金额（现金/支付宝/微信/银行卡，PayMethod=7 时使用）
    /// </summary>
    public decimal? CashAmount { get; set; }

    /// <summary>
    /// 组合支付-类别1具体方式（1:现金 2:支付宝 3:微信 4:银行卡，PayMethod=7 且 CashAmount>0 时必填）
    /// </summary>
    public int? CashPayMethod { get; set; }

    /// <summary>
    /// 组合支付-类别2储值扣款金额（PayMethod=7 时使用）
    /// </summary>
    public decimal? StoredValueAmount { get; set; }

    /// <summary>
    /// 组合支付-类别3积分抵扣金额（PayMethod=7 时使用）
    /// </summary>
    public decimal? PointsAmount { get; set; }
}
