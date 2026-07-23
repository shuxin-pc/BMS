namespace Bms.Store.Domain.Entities;

/// <summary>
/// 订单明细批次扣减记录
/// </summary>
/// <remarks>
/// 记录每个 OrderItem 实际扣减的库存批次明细，是效期销售统计（B5.5）和订单批次追溯的数据源头。
/// 替代原 OrderItem.ConsumableDeduction JSON 字段，提供结构化的批次级追溯能力。
/// 字段采用冗余快照设计：BatchNo/ExpirationDate/UnitPrice 即使原 InventoryBatch 被删除或归档也保留原值，
/// 确保订单数据永久可追溯（会计核算原则）。
/// </remarks>
public class OrderItemBatch : StoreBusinessEntityBase
{
    /// <summary>
    /// 订单明细ID（关联 OrderItem.Id，核心外键）
    /// </summary>
    public long OrderItemId { get; set; }

    /// <summary>
    /// 订单ID（冗余，便于按订单直接查询批次明细，避免多级 Join）
    /// </summary>
    public long OrderId { get; set; }

    /// <summary>
    /// 商品ID（冗余，便于按商品聚合统计效期销售）
    /// </summary>
    public long ProductId { get; set; }

    /// <summary>
    /// 库存批次ID（关联 InventoryBatch.Id）
    /// 可空：原批次可能已用完(Status=2)、已过期(Status=3)或被删除，此时仅依靠 BatchNo/ExpirationDate 冗余字段追溯
    /// </summary>
    public long? BatchId { get; set; }

    /// <summary>
    /// 批次号（冗余快照，防止批次删除后信息丢失）
    /// </summary>
    public string BatchNo { get; set; } = string.Empty;

    /// <summary>
    /// 批次过期日期（冗余快照，效期销售统计的核心字段）
    /// </summary>
    public DateTime? ExpirationDate { get; set; }

    /// <summary>
    /// 批次采购单价（冗余快照，用于精确成本计算）
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
    /// 已退款数量（默认0，支持部分退款场景）
    /// 统计实际销售数量时用 Quantity - RefundedQuantity
    /// </summary>
    public decimal RefundedQuantity { get; set; }

    /// <summary>
    /// 导航属性：订单明细
    /// </summary>
    public OrderItem? OrderItem { get; set; }

    /// <summary>
    /// 导航属性：订单
    /// </summary>
    public Order? Order { get; set; }

    /// <summary>
    /// 导航属性：商品
    /// </summary>
    public Product? Product { get; set; }

    /// <summary>
    /// 导航属性：库存批次
    /// </summary>
    public InventoryBatch? Batch { get; set; }
}
