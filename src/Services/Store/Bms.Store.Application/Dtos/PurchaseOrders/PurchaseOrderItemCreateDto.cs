namespace Bms.Store.Application.Dtos.PurchaseOrders;

/// <summary>
/// 创建采购订单明细DTO
/// </summary>
public class PurchaseOrderItemCreateDto
{
    public long PurchaseOrderId { get; set; }
    public long ProductId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public string? BatchNo { get; set; }
    /// <summary>
    /// 生产日期
    /// </summary>
    public DateTime? ProductionDate { get; set; }

    /// <summary>
    /// 保质期天数
    /// </summary>
    public int? ShelfLifeDays { get; set; }

    /// <summary>
    /// 过期日期
    /// 录入"生产日期+保质期天数"或"过期日期"二者之一；前者系统自动计算
    /// </summary>
    public DateTime? ExpirationDate { get; set; }
    public string? Remark { get; set; }
}
