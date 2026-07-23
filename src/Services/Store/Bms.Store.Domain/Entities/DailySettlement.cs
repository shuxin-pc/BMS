namespace Bms.Store.Domain.Entities;

/// <summary>
/// 日结记录
/// 店主下班前确认日结，锁定当日营业数据
/// </summary>
public class DailySettlement : StoreBusinessEntityBase
{
    /// <summary>
    /// 日结日期
    /// </summary>
    public DateTime SettlementDate { get; set; }

    /// <summary>
    /// 日结时间
    /// </summary>
    public DateTime SettlementTime { get; set; }

    /// <summary>
    /// 操作员ID
    /// </summary>
    public long? OperatorId { get; set; }

    /// <summary>
    /// 总营收（= CashRevenue + StoredValueRevenue，积分抵扣部分不计入营收）
    /// </summary>
    public decimal TotalRevenue { get; set; }

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
    /// 总退款
    /// </summary>
    public decimal TotalRefund { get; set; }

    /// <summary>
    /// 储值充值总额
    /// </summary>
    public decimal TotalStoredValueRecharge { get; set; }

    /// <summary>
    /// 储值消费总额
    /// </summary>
    public decimal TotalStoredValueConsume { get; set; }

    /// <summary>
    /// 订单数
    /// </summary>
    public int OrderCount { get; set; }

    /// <summary>
    /// 总成本（主营成本 = SalesOutboundCost + TreatmentCardOutboundCost）
    /// 仅包含销售出库与疗程卡核销出库，其他出库类型独立计入对应维度字段
    /// </summary>
    public decimal TotalCost { get; set; }

    /// <summary>
    /// 销售出库成本（订单触发的实物商品/耗材出库，SourceType=SalesOutbound）
    /// </summary>
    public decimal SalesOutboundCost { get; set; }

    /// <summary>
    /// 疗程卡核销出库成本（核销时扣减的零售商品/BOM 耗材，SourceType=TreatmentCardOutbound）
    /// </summary>
    public decimal TreatmentCardOutboundCost { get; set; }

    /// <summary>
    /// 盘亏损失（盘点盘亏出库金额，SourceType=CheckAdjustment，营业外支出）
    /// </summary>
    public decimal InventoryLossAmount { get; set; }

    /// <summary>
    /// 样品赠品费用（样品/赠品出库金额，SourceType=SampleGiftOutbound/SampleReceiveOutbound/GiftOutbound，营业外支出）
    /// </summary>
    public decimal SampleGiftAmount { get; set; }

    /// <summary>
    /// 调拨出库金额（SourceType=TransferOutbound，资产变动，不计损益）
    /// </summary>
    public decimal TransferOutAmount { get; set; }

    /// <summary>
    /// 调拨入库金额（SourceType=TransferInbound，资产变动，不计损益）
    /// </summary>
    public decimal TransferInAmount { get; set; }

    /// <summary>
    /// 采购退货金额（SourceType=PurchaseReturnOutbound，资产变动，不计损益）
    /// </summary>
    public decimal PurchaseReturnAmount { get; set; }

    /// <summary>
    /// 总毛利（总营收-总成本）
    /// </summary>
    public decimal TotalGrossProfit { get; set; }

    /// <summary>
    /// 状态（0:待确认 1:已确认）
    /// </summary>
    public int Status { get; set; } = 0;

    /// <summary>
    /// 日结记录来源：1=手动汇总，2=系统自动汇总
    /// 兜底任务遇到 Source=1 跳过，Source=2 允许覆盖更新
    /// </summary>
    public int Source { get; set; } = 2;

    /// <summary>
    /// 疗程卡核销折算金额（权责发生制：核销时将负债转营收，非售卖时一次性计入）
    /// </summary>
    public decimal TreatmentCardVerifyAmount { get; set; }

    /// <summary>
    /// 现金类退款金额（PayMethod=1-4 退款 + PayMethod=7 CashAmount 退款分摊，冲减营收）
    /// </summary>
    public decimal CashRefundAmount { get; set; }

    /// <summary>
    /// 确认人ID（System 服务用户ID）
    /// </summary>
    public long? ConfirmedBy { get; set; }

    /// <summary>
    /// 确认时间
    /// </summary>
    public DateTime? ConfirmedTime { get; set; }

    /// <summary>
    /// 反日结人ID（System 服务用户ID）
    /// </summary>
    public long? ReversedBy { get; set; }

    /// <summary>
    /// 反日结时间
    /// </summary>
    public DateTime? ReversedTime { get; set; }

    /// <summary>
    /// 反日结原因
    /// </summary>
    public string? ReversedReason { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}
