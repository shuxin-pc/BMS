namespace Bms.Store.Domain.Entities;

/// <summary>
/// 库存盘点批次明细
/// 记录一次盘点涉及的具体批次（盘盈累加批次 / 盘亏扣减批次）及各自数量，用于追溯与详情展示
/// 主表 InventoryCheck.BatchNo 仅冗余"首个影响批次"（兼容旧结构），完整明细以本表为准
/// </summary>
public class InventoryCheckBatch : StoreBusinessEntityBase
{
    /// <summary>
    /// 盘点单ID（InventoryCheck.Id）
    /// </summary>
    public long CheckId { get; set; }

    /// <summary>
    /// 库存批次ID（InventoryBatch.Id）
    /// </summary>
    public long BatchId { get; set; }

    /// <summary>
    /// 批次号（冗余存储，避免明细查询联表）
    /// </summary>
    public string BatchNo { get; set; } = string.Empty;

    /// <summary>
    /// 该批次盘点数量（盘盈为累加数量、盘亏为扣减数量，均为正数）
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// 该批次单价（冗余存储，用于金额核算与展示）
    /// </summary>
    public decimal? UnitPrice { get; set; }

    /// <summary>
    /// 该批次过期日期（冗余存储，用于展示）
    /// </summary>
    public DateTime? ExpirationDate { get; set; }

    /// <summary>
    /// 导航属性：盘点单
    /// </summary>
    public InventoryCheck? Check { get; set; }

    /// <summary>
    /// 导航属性：库存批次
    /// </summary>
    public InventoryBatch? Batch { get; set; }
}
