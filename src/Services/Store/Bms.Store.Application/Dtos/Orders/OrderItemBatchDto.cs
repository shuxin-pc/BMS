namespace Bms.Store.Application.Dtos.Orders;

/// <summary>
/// 订单明细批次扣减输出 DTO
/// </summary>
/// <remarks>
/// 用于前端订单详情展示批次效期追溯信息，以及效期销售统计（B5.5）的明细查询。
/// </remarks>
public class OrderItemBatchDto
{
    public long Id { get; set; }

    /// <summary>
    /// 订单明细ID
    /// </summary>
    public long OrderItemId { get; set; }

    /// <summary>
    /// 订单ID
    /// </summary>
    public long OrderId { get; set; }

    /// <summary>
    /// 商品ID
    /// </summary>
    public long ProductId { get; set; }

    /// <summary>
    /// 商品名称（关联查询填充，用于退款弹窗展示可退批次所属商品；默认查询不填充为 null）
    /// 注意：服务订单的服务项目行，其 OrderItemBatch.ProductId 是 BOM 耗材，ProductName 即耗材名称
    /// </summary>
    public string? ProductName { get; set; }

    /// <summary>
    /// 库存批次ID（可空：原批次可能已删除）
    /// </summary>
    public long? BatchId { get; set; }

    /// <summary>
    /// 批次号
    /// </summary>
    public string BatchNo { get; set; } = string.Empty;

    /// <summary>
    /// 批次过期日期
    /// </summary>
    public DateTime? ExpirationDate { get; set; }

    /// <summary>
    /// 批次采购单价（成本计算用）
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// 扣减数量（正数）
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// 成本金额 = Quantity × UnitPrice
    /// </summary>
    public decimal CostAmount { get; set; }

    /// <summary>
    /// 已退款数量
    /// </summary>
    public decimal RefundedQuantity { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
