namespace Bms.Store.Application.Dtos.DailySettlements;

/// <summary>
/// 日结记录 DTO
/// </summary>
public class DailySettlementDto
{
    /// <summary>
    /// 日结ID
    /// </summary>
    public long Id { get; set; }

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
    /// 总营收（= CashRevenue + StoredValueRevenue，积分抵扣部分不计入）
    /// </summary>
    public decimal TotalRevenue { get; set; }

    /// <summary>
    /// 现金类营收（现金+支付宝+微信+银行卡，含组合支付类别1部分）
    /// </summary>
    public decimal CashRevenue { get; set; }

    /// <summary>
    /// 储值扣款营收（含组合支付类别2部分）
    /// </summary>
    public decimal StoredValueRevenue { get; set; }

    /// <summary>
    /// 积分抵扣金额（仅记录，不纳入营收）
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
    /// </summary>
    public decimal TotalCost { get; set; }

    /// <summary>
    /// 销售出库成本（订单触发的实物商品/耗材出库）
    /// </summary>
    public decimal SalesOutboundCost { get; set; }

    /// <summary>
    /// 项目卡核销出库成本（核销时扣减的零售商品/BOM 耗材）
    /// </summary>
    public decimal TreatmentCardOutboundCost { get; set; }

    /// <summary>
    /// 盘亏损失（营业外支出）
    /// </summary>
    public decimal InventoryLossAmount { get; set; }

    /// <summary>
    /// 样品赠品费用（营业外支出）
    /// </summary>
    public decimal SampleGiftAmount { get; set; }

    /// <summary>
    /// 调拨出库金额（资产变动，不计损益）
    /// </summary>
    public decimal TransferOutAmount { get; set; }

    /// <summary>
    /// 调拨入库金额（资产变动，不计损益）
    /// </summary>
    public decimal TransferInAmount { get; set; }

    /// <summary>
    /// 采购退货金额（资产变动，不计损益）
    /// </summary>
    public decimal PurchaseReturnAmount { get; set; }

    /// <summary>
    /// 总毛利（总营收-总成本，未扣退款）
    /// </summary>
    public decimal TotalGrossProfit { get; set; }

    /// <summary>
    /// 净营收 = 总营收 - 总退款（可为负，表示退款大于营收）
    /// </summary>
    public decimal NetRevenue => TotalRevenue - TotalRefund;

    /// <summary>
    /// 退款率 = 总退款 / 总营收（营收为0时返回0）
    /// </summary>
    public decimal RefundRatio => TotalRevenue > 0 ? TotalRefund / TotalRevenue : 0m;

    /// <summary>
    /// 退款是否大于营收（true 表示异常，前端需高亮提醒）
    /// </summary>
    public bool IsRefundExceedRevenue => TotalRefund > TotalRevenue;

    /// <summary>
    /// 营业外支出合计 = 盘亏损失 + 样品赠品费用
    /// </summary>
    public decimal TotalOperatingExpense => InventoryLossAmount + SampleGiftAmount;

    /// <summary>
    /// 资产变动净额 = 调拨出库 - 调拨入库 + 采购退货（不计损益，仅资产层面）
    /// </summary>
    public decimal NetTransferAmount => TransferOutAmount - TransferInAmount + PurchaseReturnAmount;

    /// <summary>
    /// 净毛利 = 净营收 - 总成本（扣除退款后的真实毛利，可为负）
    /// </summary>
    public decimal NetGrossProfit => NetRevenue - TotalCost;

    /// <summary>
    /// 状态（0:待确认 1:已确认）
    /// </summary>
    public int Status { get; set; }

    /// <summary>
    /// 日结记录来源：1=手动汇总，2=系统自动汇总
    /// </summary>
    public int Source { get; set; }

    /// <summary>
    /// 项目卡核销折算金额（权责发生制转营收）
    /// </summary>
    public decimal TreatmentCardVerifyAmount { get; set; }

    /// <summary>
    /// 现金类退款金额（冲减营收）
    /// </summary>
    public decimal CashRefundAmount { get; set; }

    /// <summary>
    /// 确认人ID
    /// </summary>
    public long? ConfirmedBy { get; set; }

    /// <summary>
    /// 确认时间
    /// </summary>
    public DateTime? ConfirmedTime { get; set; }

    /// <summary>
    /// 反日结人ID
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

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}
