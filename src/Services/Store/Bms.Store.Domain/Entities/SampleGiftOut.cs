namespace Bms.Store.Domain.Entities;

/// <summary>
/// 赠品出库记录
/// </summary>
public class SampleGiftOut : StoreBusinessEntityBase
{
    /// <summary>
    /// 商品ID（关联 Product.Id，样品/赠品作为商品档案的子类型）
    /// </summary>
    public long ProductId { get; set; }

    /// <summary>
    /// 出库批次ID（指定从哪个 InventoryBatch 扣减库存）
    /// </summary>
    public long InventoryBatchId { get; set; }

    /// <summary>
    /// 出库数量
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// 出库时间
    /// </summary>
    public DateTime OutTime { get; set; }

    /// <summary>
    /// 关联活动ID（如促销活动的ID）
    /// </summary>
    public long? ActivityId { get; set; }

    /// <summary>
    /// 关联订单ID（如买一送一活动的主订单）
    /// </summary>
    public long? OrderId { get; set; }

    /// <summary>
    /// 操作员ID
    /// </summary>
    public long? OperatorId { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 导航属性：商品
    /// </summary>
    public Product? Product { get; set; }

    /// <summary>
    /// 导航属性：库存批次
    /// </summary>
    public InventoryBatch? InventoryBatch { get; set; }
}
