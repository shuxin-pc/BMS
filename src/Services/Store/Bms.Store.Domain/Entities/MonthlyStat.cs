namespace Bms.Store.Domain.Entities;

/// <summary>
/// 月统计实体
/// 月统计数据 = SUM(日统计数据)
/// </summary>
public class MonthlyStat : StoreBusinessEntityBase
{
    /// <summary>
    /// 统计月份（如：2026-07）
    /// </summary>
    public string StatMonth { get; set; } = string.Empty;

    /// <summary>
    /// 营收金额（= CashRevenue + StoredValueRevenue，积分抵扣部分不计入营收）
    /// </summary>
    public decimal Revenue { get; set; }

    /// <summary>
    /// 现金类营收（现金+支付宝+微信+银行卡，含组合支付类别1部分）
    /// </summary>
    public decimal CashRevenue { get; set; }

    /// <summary>
    /// 储值扣款营收（含组合支付类别2部分，按实收金额计）
    /// </summary>
    public decimal StoredValueRevenue { get; set; }

    /// <summary>
    /// 积分抵扣金额（仅记录，不纳入营收，避免重复计算）
    /// </summary>
    public decimal PointsDeductAmount { get; set; }

    /// <summary>
    /// 商品成本（FIFO计算）
    /// </summary>
    public decimal Cost { get; set; }

    /// <summary>
    /// 毛利（营收-成本）
    /// </summary>
    public decimal GrossProfit { get; set; }

    /// <summary>
    /// 订单数
    /// </summary>
    public int OrderCount { get; set; }

    /// <summary>
    /// 退款金额
    /// </summary>
    public decimal RefundAmount { get; set; }

    /// <summary>
    /// 储值充值金额
    /// </summary>
    public decimal StoredValueRecharge { get; set; }

    /// <summary>
    /// 储值消费金额
    /// </summary>
    public decimal StoredValueConsume { get; set; }

    /// <summary>
    /// 项目卡核销折算金额（权责发生制：核销时将负债转营收，非售卖时一次性计入）
    /// </summary>
    public decimal TreatmentCardVerifyAmount { get; set; }

    /// <summary>
    /// 消费客户数（去重，月度汇总）
    /// </summary>
    public int ConsumeCustomerCount { get; set; }

    /// <summary>
    /// 新客数（月度汇总）
    /// </summary>
    public int NewCustomerCount { get; set; }
}
