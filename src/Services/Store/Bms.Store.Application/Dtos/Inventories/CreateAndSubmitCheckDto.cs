namespace Bms.Store.Application.Dtos.Inventories;

/// <summary>
/// 创建并提交盘点单输入 DTO（原子操作）
/// 合并 Create + Submit 两步为一个事务，避免草稿残留
/// 按差异方向分支：盘亏用 DeductBatches，盘盈用 GainBatchNo，无差异时全部忽略
/// </summary>
public class CreateAndSubmitCheckDto
{
    /// <summary>
    /// 商品ID
    /// </summary>
    public long ProductId { get; set; }

    /// <summary>
    /// 盘点前账面库存（前端从商品选项带出）
    /// </summary>
    public decimal BeforeQuantity { get; set; }

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
    /// 盘盈批次号（差异为正时使用，必须为当前商品在当前门店的已有批次号）
    /// </summary>
    public string? GainBatchNo { get; set; }

    /// <summary>
    /// 备注（可选）
    /// </summary>
    public string? Remark { get; set; }
}
