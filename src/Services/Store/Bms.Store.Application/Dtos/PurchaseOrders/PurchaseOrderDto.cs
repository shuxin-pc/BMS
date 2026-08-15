namespace Bms.Store.Application.Dtos.PurchaseOrders;

/// <summary>
/// 采购订单DTO
/// </summary>
public class PurchaseOrderDto
{
    public long Id { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public int Status { get; set; }
    public int PurchaseType { get; set; }
    public long? OperatorId { get; set; }

    /// <summary>
    /// 操作员姓名（冗余存储，展示用）
    /// </summary>
    public string? OperatorName { get; set; }

    public string? Remark { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// 采购明细列表
    /// </summary>
    public List<PurchaseOrderItemDto> Items { get; set; } = new();
}
