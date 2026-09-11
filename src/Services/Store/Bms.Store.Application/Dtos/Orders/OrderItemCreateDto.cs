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

    /// <summary>
    /// 服务开始时间（服务内容弹窗/核销项目录入的真实服务开始时间，可空）
    /// 用于资源占用检测与技师统计归集，为空时后端回退 OrderTime + Duration 推算
    /// </summary>
    public DateTime? ServiceStartTime { get; set; }

    /// <summary>
    /// 服务结束时间（服务开始时间 + 服务时长自动计算，可空）
    /// </summary>
    public DateTime? ServiceEndTime { get; set; }
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

    /// <summary>
    /// 关联活动ID（可选，仅赠品项 Type=5 有意义，用于活动维度归因统计）。
    /// 非必填，传入时后端校验活动存在且未删除，写入 InventoryLog.ActivityId。
    /// </summary>
    public long? ActivityId { get; set; }

    /// <summary>
    /// 服务项目绑定耗材的效期选择（仅服务项目 Type=2 有意义）。
    /// 加购服务项目时店员选择绑定耗材的效期；空表示未绑定耗材或系统自动按 FEFO 扣减。
    /// 后端 DeductServiceBomAsync 按指定效期扣减对应耗材库存。
    /// </summary>
    public List<ConsumableExpiryInput> ConsumableExpiries { get; set; } = new();
}
