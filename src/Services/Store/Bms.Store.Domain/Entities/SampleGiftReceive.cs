namespace Bms.Store.Domain.Entities;

/// <summary>
/// 样品领用记录
/// </summary>
public class SampleGiftReceive : StoreBusinessEntityBase
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
    /// 客户ID（可选，B6.2 需求要求客户可选，支持无客户领样场景）
    /// </summary>
    public long? CustomerId { get; set; }

    /// <summary>
    /// 领取数量
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// 领取时间
    /// </summary>
    public DateTime ReceiveTime { get; set; }

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
    /// 导航属性：客户
    /// </summary>
    public Customer? Customer { get; set; }

    /// <summary>
    /// 导航属性：库存批次
    /// </summary>
    public InventoryBatch? InventoryBatch { get; set; }
}
