namespace Bms.Store.Domain.Entities;

/// <summary>
/// 库存汇总表
/// 每商品每门店一条记录，Quantity 为所有在库批次数量之和
/// 批次级信息（过期日期、批次号、采购单价）请查 InventoryBatch
/// </summary>
public class Inventory : StoreBusinessEntityBase
{
    /// <summary>
    /// 商品ID
    /// </summary>
    public long ProductId { get; set; }

    /// <summary>
    /// 当前库存数量（= Σ 在库批次 Quantity）
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// 导航属性：商品
    /// </summary>
    public Product? Product { get; set; }
}
