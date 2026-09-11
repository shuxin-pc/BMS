namespace Bms.Store.Application.Dtos.Inventories;

/// <summary>
/// 库存盘点记录输出 DTO
/// </summary>
public class InventoryCheckDto
{
    public long Id { get; set; }
    public long ProductId { get; set; }
    public decimal BeforeQuantity { get; set; }
    public decimal ActualQuantity { get; set; }
    public decimal DiffQuantity { get; set; }
    public DateTime CheckTime { get; set; }
    public long? OperatorId { get; set; }

    /// <summary>
    /// 操作员姓名（冗余存储，创建时取 ICurrentUser.RealName ?? UserName）
    /// </summary>
    public string? OperatorName { get; set; }

    /// <summary>
    /// 商品名称（联表 Product 查询填充）
    /// </summary>
    public string? ProductName { get; set; }

    /// <summary>
    /// 商品编码（联表 Product 查询填充）
    /// </summary>
    public string? ProductCode { get; set; }

    /// <summary>
    /// 商品成本价（盘亏时取首批扣减批次单价；盘盈时取录入估值单价；来自 InventoryCheck.UnitPrice 持久化字段）
    /// </summary>
    public decimal? UnitCost { get; set; }

    /// <summary>
    /// 差异金额（= DiffQuantity × 批次实际单价，正数为盘盈金额，负数为盘亏金额）
    /// 提交时由后端按真实批次单价计算并持久化，不再查询时临时算
    /// </summary>
    public decimal? DiffAmount { get; set; }

    /// <summary>
    /// 批次号（盘亏时取首批扣减批次号；盘盈时取新建盘盈批次号）
    /// </summary>
    public string? BatchNo { get; set; }

    /// <summary>
    /// 过期日期（盘亏时取首批扣减批次过期日期；盘盈时取录入过期日期）
    /// </summary>
    public DateTime? ExpirationDate { get; set; }

    /// <summary>
    /// 盘点单状态（0=草稿 1=已完成 2=已取消）
    /// </summary>
    public int Status { get; set; }

    public string? Remark { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// 本次盘点涉及批次明细（盘盈累加批次 / 盘亏扣减批次）
    /// 为空表示无差异（未调整库存）
    /// </summary>
    public List<InventoryCheckBatchDto> Batches { get; set; } = new();
}

/// <summary>
/// 库存盘点批次明细 DTO（盘盈累加批次 / 盘亏扣减批次）
/// 对应实体 InventoryCheckBatch
/// </summary>
public class InventoryCheckBatchDto
{
    /// <summary>
    /// 明细ID
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 库存批次ID（InventoryBatch.Id）
    /// </summary>
    public long BatchId { get; set; }

    /// <summary>
    /// 批次号
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
}
