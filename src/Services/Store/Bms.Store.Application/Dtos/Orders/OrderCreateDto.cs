namespace Bms.Store.Application.Dtos.Orders;

/// <summary>
/// 创建订单输入 DTO
/// </summary>
public class OrderCreateDto
{
    public string OrderNo { get; set; } = string.Empty;
    public long? CustomerId { get; set; }
    public int OrderType { get; set; } = 1;
    public int Status { get; set; } = 1;
    public int BackfillStatus { get; set; }
    public decimal ProductAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public int Points { get; set; }
    public int? PayMethod { get; set; }

    /// <summary>
    /// 组合支付-类别1金额（现金/支付宝/微信/银行卡，PayMethod=7 时使用）
    /// </summary>
    public decimal? CashAmount { get; set; }

    /// <summary>
    /// 组合支付-类别1具体方式（1:现金 2:支付宝 3:微信 4:银行卡）
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

    public DateTime OrderTime { get; set; }
    public DateTime? CompleteTime { get; set; }
    public decimal RefundAmount { get; set; }
    public DateTime? RefundTime { get; set; }
    public string? RefundReason { get; set; }
    public long? OperatorId { get; set; }
    public string? Remark { get; set; }

    /// <summary>
    /// 订单明细列表（级联创建，CreateAsync 中按 OrderType 联动库存/疗程卡/储值）
    /// </summary>
    public List<OrderItemCreateDto> Items { get; set; } = new();

    /// <summary>
    /// 疗程卡销售ID（仅 OrderType=3 疗程卡核销时需要，用于关联疗程卡销售记录）
    /// </summary>
    public long? CardSaleId { get; set; }

    /// <summary>
    /// 源预约ID（预约转订单时由前端传入）
    /// 设置后 OrderAppService.CreateAsync 会从源预约复制 TechnicianId/RoomId/EquipmentId 到 OrderItem
    /// 并跳过资源冲突检测（预约创建时已验证，订单直接继承占用）
    /// </summary>
    public long? SourceAppointmentId { get; set; }
}
