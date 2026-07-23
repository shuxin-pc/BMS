namespace Bms.Store.Application.Dtos.Orders;

/// <summary>
/// 订单明细输出 DTO
/// </summary>
public class OrderItemDto
{
    public long Id { get; set; }
    public long OrderId { get; set; }
    public long ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public long? TechnicianId { get; set; }
    public int? TechnicianSource { get; set; }

    /// <summary>
    /// 技师姓名（关联查询填充，用于前端展示）
    /// </summary>
    public string? TechnicianName { get; set; }

    /// <summary>
    /// 房间/床位ID
    /// </summary>
    public long? RoomId { get; set; }

    /// <summary>
    /// 房间/床位名称（关联查询填充，用于前端展示）
    /// </summary>
    public string? RoomName { get; set; }

    /// <summary>
    /// 设备ID
    /// </summary>
    public long? EquipmentId { get; set; }

    /// <summary>
    /// 设备名称（关联查询填充，用于前端展示）
    /// </summary>
    public string? EquipmentName { get; set; }

    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal DiscountRate { get; set; }
    public decimal DiscountedAmount { get; set; }
    public decimal? TechnicianFee { get; set; }

    /// <summary>
    /// 耗材成本（服务订单按BOM扣减耗材的归集成本）
    /// </summary>
    public decimal? ConsumableCost { get; set; }

    /// <summary>
    /// 批次扣减明细列表（含效期、数量、单价，用于订单详情展示批次追溯信息）
    /// 替代原 ConsumableDeduction JSON 字段
    /// </summary>
    public List<OrderItemBatchDto> Batches { get; set; } = new();

    public string? Remark { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
