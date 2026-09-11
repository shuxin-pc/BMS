namespace Bms.Store.Domain.Entities;

/// <summary>
/// 项目卡销售记录
/// </summary>
public class TreatmentCardSale : StoreEntity
{
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
    /// 累计已消费金额（按折算单价累计，用于"最后一次核销兜底"和退款计算）
    /// </summary>
    public decimal TotalConsumedAmount { get; set; }

    /// <summary>
    /// 剩余次数
    /// </summary>
    public int RemainingTimes { get; set; }

    /// <summary>
    /// 有效期至
    /// </summary>
    public DateTime ExpiryDate { get; set; }

    /// <summary>
    /// 状态（1:有效 2:已用完 3:已过期）
    /// </summary>
    public int Status { get; set; } = 1;

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 购物车结算批次号（POS 购物车一次结算生成，用于订单/核销/开卡三单据聚合追溯）
    /// 非购物车结算（独立开卡）为空
    /// </summary>
    public string? CheckoutSessionNo { get; set; }

    /// <summary>
    /// 销售单号（后端自动生成，格式：TC{yyyyMMdd}{序号}，如 TC20260821001）
    /// 历史数据为空（迁移新增列，不回填）
    /// </summary>
    public string? SaleNo { get; set; }

    /// <summary>
    /// 支付方式（1:现金 2:支付宝 3:微信 4:银行卡 5:储值卡 6:积分抵扣 7:组合支付）
    /// 单一支付方式时使用 1-6；组合支付使用 7，并通过 CashAmount/StoredValueAmount/PointsAmount 拆分。
    /// 与订单 Order.PayMethod 枚举口径一致（POS 统一支付方式，开卡与订单必然相同）
    /// </summary>
    public int? PayMethod { get; set; }

    /// <summary>
    /// 组合支付-类别1金额（现金/支付宝/微信/银行卡，PayMethod=7 时使用）
    /// 类别1为线下或第三方收款，系统不联动扣减，仅记录金额与具体方式
    /// </summary>
    public decimal? CashAmount { get; set; }

    /// <summary>
    /// 组合支付-类别1具体方式（1:现金 2:支付宝 3:微信 4:银行卡，PayMethod=7 且 CashAmount>0 时必填）
    /// </summary>
    public int? CashPayMethod { get; set; }

    /// <summary>
    /// 组合支付-类别2储值扣款金额（PayMethod=7 时使用，由 StoredValueAccountAppService.ConsumeAsync 扣减）
    /// </summary>
    public decimal? StoredValueAmount { get; set; }

    /// <summary>
    /// 组合支付-类别3积分抵扣金额（PayMethod=7 时使用，按门店 PointsRule.DeductRate 换算积分扣减）
    /// 积分抵扣部分不纳入营收统计，亦不发放积分
    /// </summary>
    public decimal? PointsAmount { get; set; }

    /// <summary>
    /// 开卡发放积分快照（购买时按发积分基数 × PointsRate 一次性发放，Floor 取整）
    /// 用于退卡时按应退比例扣回发放积分（参照 Order.Points 快照设计），历史数据为空
    /// </summary>
    public int AwardedPoints { get; set; }

    /// <summary>
    /// 导航属性：项目卡
    /// </summary>
    public TreatmentCard? Card { get; set; }

    /// <summary>
    /// 导航属性：客户
    /// </summary>
    public Customer? Customer { get; set; }

    /// <summary>
    /// 导航属性：项目卡包含的项目明细（购买时按实际售价分摊计算折算单价并锁定）
    /// </summary>
    public List<TreatmentCardSaleItem> Items { get; set; } = new();
}
