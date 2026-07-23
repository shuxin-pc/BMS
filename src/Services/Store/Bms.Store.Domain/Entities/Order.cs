namespace Bms.Store.Domain.Entities;

/// <summary>
/// 订单类型常量定义
/// OrderType 仅允许 1(零售)、2(服务)、3(疗程卡核销) 三种，
/// 储值消费不作为订单类型，而是通过 PayMethod=5(储值卡) 作为支付方式。
/// 疗程卡核销订单（Type=3）禁止通过订单接口创建，必须走 TreatmentCardVerifyAppService.CreateAsync。
/// </summary>
public static class OrderTypes
{
    public const int Retail = 1;                  // 零售
    public const int Service = 2;                 // 服务
    public const int TreatmentCardVerify = 3;     // 疗程卡核销

    /// <summary>
    /// 校验订单类型是否合法
    /// </summary>
    public static bool IsValid(int type) => type == Retail || type == Service || type == TreatmentCardVerify;
}

/// <summary>
/// 订单
/// </summary>
public class Order : StoreBusinessEntityBase
{
    /// <summary>
    /// 订单编号
    /// </summary>
    public string OrderNo { get; set; } = string.Empty;

    /// <summary>
    /// 客户ID
    /// </summary>
    public long? CustomerId { get; set; }

    /// <summary>
    /// 订单类型（1:零售 2:服务 3:疗程卡核销）
    /// 储值消费不作为订单类型，而是通过 PayMethod=5(储值卡) 作为支付方式
    /// </summary>
    public int OrderType { get; set; } = 1;

    /// <summary>
    /// 订单状态（1:进行中 2:已完成 3:已退款 4:已取消）
    /// </summary>
    public int Status { get; set; } = 1;

    /// <summary>
    /// 补录状态（0:正常 1:补录）
    /// </summary>
    public int BackfillStatus { get; set; }

    /// <summary>
    /// 商品总金额
    /// </summary>
    public decimal ProductAmount { get; set; }

    /// <summary>
    /// 折扣金额
    /// </summary>
    public decimal DiscountAmount { get; set; }

    /// <summary>
    /// 实收金额
    /// </summary>
    public decimal PaidAmount { get; set; }

    /// <summary>
    /// 获得积分
    /// </summary>
    public int Points { get; set; }

    /// <summary>
    /// 支付方式（1:现金 2:支付宝 3:微信 4:银行卡 5:储值卡 6:积分抵扣 7:组合支付）
    /// 单一支付方式时使用 1-6；组合支付使用 7，并通过 CashAmount/StoredValueAmount/PointsAmount 拆分。
    /// 历史订单 PayMethod=1~6 保持向后兼容。
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
    /// 先扣实收余额(RealBalance)，不足部分扣赠送余额(GiftBalance)
    /// </summary>
    public decimal? StoredValueAmount { get; set; }

    /// <summary>
    /// 组合支付-类别3积分抵扣金额（PayMethod=7 时使用，按门店 PointsRule.DeductRate 换算积分扣减）
    /// 积分抵扣部分不纳入营收统计，亦不发放积分
    /// </summary>
    public decimal? PointsAmount { get; set; }

    /// <summary>
    /// 下单时间
    /// </summary>
    public DateTime OrderTime { get; set; }

    /// <summary>
    /// 完成时间
    /// </summary>
    public DateTime? CompleteTime { get; set; }

    /// <summary>
    /// 退款金额
    /// </summary>
    public decimal RefundAmount { get; set; }

    /// <summary>
    /// 退款时间
    /// </summary>
    public DateTime? RefundTime { get; set; }

    /// <summary>
    /// 退款原因
    /// </summary>
    public string? RefundReason { get; set; }

    /// <summary>
    /// 操作员ID（System服务用户）
    /// </summary>
    public long? OperatorId { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 导航属性：客户
    /// </summary>
    public Customer? Customer { get; set; }

    /// <summary>
    /// 导航属性：订单明细列表
    /// </summary>
    public List<OrderItem> OrderItems { get; set; } = new();
}
