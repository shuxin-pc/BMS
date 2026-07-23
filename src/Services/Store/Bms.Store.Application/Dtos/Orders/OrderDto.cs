namespace Bms.Store.Application.Dtos.Orders;

/// <summary>
/// 订单输出 DTO
/// </summary>
public class OrderDto
{
    public long Id { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public long? CustomerId { get; set; }
    public int OrderType { get; set; }
    public int Status { get; set; }
    public int BackfillStatus { get; set; }
    public decimal ProductAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public int Points { get; set; }
    public int? PayMethod { get; set; }

    /// <summary>
    /// 组合支付-类别1金额（现金/支付宝/微信/银行卡，PayMethod=7 时返回）
    /// </summary>
    public decimal? CashAmount { get; set; }

    /// <summary>
    /// 组合支付-类别1具体方式（1:现金 2:支付宝 3:微信 4:银行卡）
    /// </summary>
    public int? CashPayMethod { get; set; }

    /// <summary>
    /// 组合支付-类别2储值扣款金额（PayMethod=7 时返回）
    /// </summary>
    public decimal? StoredValueAmount { get; set; }

    /// <summary>
    /// 组合支付-类别3积分抵扣金额（PayMethod=7 时返回）
    /// </summary>
    public decimal? PointsAmount { get; set; }

    public DateTime OrderTime { get; set; }
    public DateTime? CompleteTime { get; set; }
    public decimal RefundAmount { get; set; }
    public DateTime? RefundTime { get; set; }
    public string? RefundReason { get; set; }
    public long? OperatorId { get; set; }
    public string? Remark { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// 订单明细列表
    /// </summary>
    public List<OrderItemDto> Items { get; set; } = new();
}
