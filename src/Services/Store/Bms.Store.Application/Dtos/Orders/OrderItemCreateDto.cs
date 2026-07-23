namespace Bms.Store.Application.Dtos.Orders;

/// <summary>
/// 创建订单明细输入 DTO
/// </summary>
public class OrderItemCreateDto
{
    public long OrderId { get; set; }
    public long ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public long? TechnicianId { get; set; }
    public int? TechnicianSource { get; set; }

    /// <summary>
    /// 房间/床位ID（服务订单占用房间资源，可空）
    /// 预约转订单时由调用方从 Appointment.RoomId 传入
    /// </summary>
    public long? RoomId { get; set; }

    /// <summary>
    /// 设备ID（服务订单占用设备资源，可空）
    /// </summary>
    public long? EquipmentId { get; set; }
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal DiscountRate { get; set; } = 1.0m;
    public decimal DiscountedAmount { get; set; }
    public decimal? TechnicianFee { get; set; }

    /// <summary>
    /// 店员选择的效期列表（按扣减顺序）。
    /// 为空表示系统自动按近效期扣减（FEFO）；
    /// 非空时按顺序依次扣减，同效期多批次按 PurchaseDate 升序（FIFO）逐批扣减。
    /// 元素为 null 表示选择"无效期限制"批次，按 CreatedTime 升序扣减。
    /// </summary>
    public List<DateTime?> ExpirationDates { get; set; } = new();

    /// <summary>
    /// 当 ExpirationDates 非空且选中效期库存不足时，是否允许系统自动从其他近效期批次补足缺口。
    /// 默认 false：不足时返回 409，由前端弹窗让店员决定。
    /// true：不足部分按 FEFO 自动从其他效期扣减。
    /// </summary>
    public bool AllowAutoFillBeyondSelection { get; set; } = false;

    public string? Remark { get; set; }
}
