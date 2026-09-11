namespace Bms.Store.Domain.Entities;

/// <summary>
/// 订单明细
/// </summary>
public class OrderItem : StoreBusinessEntityBase
{
    /// <summary>
    /// 订单ID
    /// </summary>
    public long OrderId { get; set; }

    /// <summary>
    /// 商品ID
    /// </summary>
    public long ProductId { get; set; }

    /// <summary>
    /// 商品名称（冗余存储）
    /// </summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// 商品编码（冗余存储）
    /// </summary>
    public string ProductCode { get; set; } = string.Empty;

    /// <summary>
    /// 技师ID（服务商品时选择）
    /// </summary>
    public long? TechnicianId { get; set; }

    /// <summary>
    /// 技师来源（1:商家技师 2:平台技师）
    /// </summary>
    public int? TechnicianSource { get; set; }

    /// <summary>
    /// 房间/床位ID（服务订单占用房间资源，与 Appointment.RoomId 语义一致）
    /// 预约转订单时从 Appointment.RoomId 复制；直接开单时由门店人员选择
    /// </summary>
    public long? RoomId { get; set; }

    /// <summary>
    /// 设备ID（服务订单占用设备资源，与 Appointment.EquipmentId 语义一致）
    /// </summary>
    public long? EquipmentId { get; set; }

    /// <summary>
    /// 服务开始时间（服务内容弹窗/核销项目录入的真实服务开始时间，可空）
    /// 用于资源占用检测与技师统计归集（StatDate=ServiceStartTime.Date），为空时回退 OrderTime + Duration 推算
    /// </summary>
    public DateTime? ServiceStartTime { get; set; }

    /// <summary>
    /// 服务结束时间（服务开始时间 + 服务时长自动计算，可空）
    /// </summary>
    public DateTime? ServiceEndTime { get; set; }

    /// <summary>
    /// 数量
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// 单价
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// 折扣率
    /// </summary>
    public decimal DiscountRate { get; set; } = 1.0m;

    /// <summary>
    /// 折扣后金额
    /// </summary>
    public decimal DiscountedAmount { get; set; }

    /// <summary>
    /// 技师服务费用（可选，由门店录入）
    /// </summary>
    public decimal? TechnicianFee { get; set; }

    /// <summary>
    /// 耗材成本（服务订单按BOM扣减耗材的归集成本，= Σ(OrderItemBatch.CostAmount)）
    /// </summary>
    public decimal? ConsumableCost { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 导航属性：订单
    /// </summary>
    public Order? Order { get; set; }

    /// <summary>
    /// 导航属性：商品
    /// </summary>
    public Product? Product { get; set; }

    /// <summary>
    /// 导航属性：批次扣减明细列表
    /// 记录本明细实际扣减的库存批次（含效期、数量、单价），是效期追溯和成本核算的结构化数据源。
    /// 替代原 ConsumableDeduction JSON 字段。
    /// </summary>
    public List<OrderItemBatch> Batches { get; set; } = new();
}
