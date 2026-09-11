namespace Bms.Store.Application.Dtos.Inventories;

/// <summary>
/// 提交盘点单输入 DTO
/// 草稿状态的盘点单通过此 DTO 录入实际数量并提交，系统按差异方向分支处理：
/// - 盘亏（DiffQuantity &lt; 0）：扣减指定批次或 FIFO 兜底，按批次实际单价累加 DiffAmount
/// - 盘盈（DiffQuantity &gt; 0）：新建盘盈批次，按录入单价计算 DiffAmount
/// - 无差异：仅更新状态，不调整库存
/// </summary>
public class SubmitCheckDto
{
    /// <summary>
    /// 盘点单ID
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 实际盘点数量
    /// </summary>
    public decimal ActualQuantity { get; set; }

    /// <summary>
    /// 盘亏批次扣减明细（差异为负时使用）
    /// 为空 -> 后端按 FIFO 兜底扣减
    /// 非空 -> 按操作员指定批次扣减，校验数量合计与差异数量匹配
    /// </summary>
    public List<BatchDeductItem>? DeductBatches { get; set; }

    /// <summary>
    /// 盘盈批次累加明细（差异为正时使用，可多个批次）
    /// 每个批次分别录入累加数量，数量合计必须与差异数量匹配
    /// </summary>
    public List<GainBatchItem>? GainBatches { get; set; }

    /// <summary>
    /// 盘盈批次单价（差异为正时使用，留空取 Product.CostPrice）
    /// </summary>
    public decimal? GainUnitPrice { get; set; }

    /// <summary>
    /// 盘盈批次生产日期（差异为正时使用，可选）
    /// </summary>
    public DateTime? GainProductionDate { get; set; }

    /// <summary>
    /// 盘盈批次保质期天数（差异为正时使用，可选）
    /// 与生产日期联动计算过期日期：过期日期 = 生产日期 + 保质期天数
    /// </summary>
    public int? GainShelfLifeDays { get; set; }

    /// <summary>
    /// 盘盈批次过期日期（差异为正时使用，可选）
    /// </summary>
    public DateTime? GainExpirationDate { get; set; }

    /// <summary>
    /// 备注（可选）
    /// </summary>
    public string? Remark { get; set; }
}

/// <summary>
/// 批次扣减明细项（盘亏指定批次模式时使用）
/// </summary>
public class BatchDeductItem
{
    /// <summary>
    /// 批次ID（InventoryBatch.Id）
    /// </summary>
    public long BatchId { get; set; }

    /// <summary>
    /// 该批次扣减数量，必须 &gt; 0
    /// </summary>
    public decimal Quantity { get; set; }
}

/// <summary>
/// 盘盈批次累加明细项（盘盈累加批次时使用）
/// </summary>
public class GainBatchItem
{
    /// <summary>
    /// 批次ID（InventoryBatch.Id）
    /// </summary>
    public long BatchId { get; set; }

    /// <summary>
    /// 该批次累加数量，必须 &gt; 0
    /// </summary>
    public decimal Quantity { get; set; }
}
