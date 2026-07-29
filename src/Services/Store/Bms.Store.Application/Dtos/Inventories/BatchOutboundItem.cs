namespace Bms.Store.Application.Dtos.Inventories;

/// <summary>
/// 手动指定批次扣减明细（出库手动模式使用）
/// </summary>
public class BatchOutboundItem
{
    /// <summary>批次ID（InventoryBatch.Id）</summary>
    public long BatchId { get; set; }

    /// <summary>该批次扣减数量（>0）</summary>
    public decimal Quantity { get; set; }
}