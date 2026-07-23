namespace Bms.Store.Domain.Entities;

/// <summary>
/// 日统计实体
/// 每日日结时生成当日统计数据，支撑首页看板和月度汇总
/// </summary>
public class DailyStat : StoreBusinessEntityBase
{
    /// <summary>
    /// 统计日期
    /// </summary>
    public DateTime StatDate { get; set; }

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
    /// 商品成本（主营成本 = SalesOutboundCost + TreatmentCardOutboundCost）
    /// 仅包含销售出库与疗程卡核销出库，其他出库类型独立计入对应维度字段
    /// </summary>
    public decimal Cost { get; set; }

    /// <summary>
    /// 销售出库成本（订单触发的实物商品/耗材出库，SourceType=SalesOutbound）
    /// </summary>
    public decimal SalesOutboundCost { get; set; }

    /// <summary>
    /// 疗程卡核销出库成本（核销时扣减的零售商品/BOM 耗材，SourceType=TreatmentCardOutbound）
    /// </summary>
    public decimal TreatmentCardOutboundCost { get; set; }

    /// <summary>
    /// 盘亏损失（盘点盘亏出库金额，营业外支出）
    /// </summary>
    public decimal InventoryLossAmount { get; set; }

    /// <summary>
    /// 样品赠品费用（样品/赠品出库金额，营业外支出）
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
    /// 现金类退款金额（PayMethod=1-4 退款 + PayMethod=7 CashAmount 退款分摊，冲减营收）
    /// </summary>
    public decimal CashRefundAmount { get; set; }

    /// <summary>
    /// 储值充值金额
    /// </summary>
    public decimal StoredValueRecharge { get; set; }

    /// <summary>
    /// 储值消费金额
    /// </summary>
    public decimal StoredValueConsume { get; set; }

    /// <summary>
    /// 今日消费客户数（去重）
    /// </summary>
    public int ConsumeCustomerCount { get; set; }

    /// <summary>
    /// 今日新客数
    /// </summary>
    public int NewCustomerCount { get; set; }

    /// <summary>
    /// 今日预约数
    /// </summary>
    public int AppointmentCount { get; set; }

    /// <summary>
    /// 库存预警数
    /// </summary>
    public int InventoryAlertCount { get; set; }

    /// <summary>
    /// 疗程卡核销折算金额（权责发生制：核销时将负债转营收，非售卖时一次性计入）
    /// </summary>
    public decimal TreatmentCardVerifyAmount { get; set; }
}
