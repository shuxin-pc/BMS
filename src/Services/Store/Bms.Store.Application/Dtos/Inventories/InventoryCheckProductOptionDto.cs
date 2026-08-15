namespace Bms.Store.Application.Dtos.Inventories;

/// <summary>
/// 盘点专用商品选项（含账面库存、成本价、在库批次列表，用于新增盘点时下拉选择）
/// </summary>
public class InventoryCheckProductOptionDto
{
    public long Id { get; set; }

    /// <summary>
    /// 商品名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 商品编码
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 单位
    /// </summary>
    public string? Unit { get; set; }

    /// <summary>
    /// 账面库存（当前门店该商品库存量）
    /// </summary>
    public decimal Stock { get; set; }

    /// <summary>
    /// 成本价（来自 Product.CostPrice，盘盈时作为默认估值单价）
    /// </summary>
    public decimal? CostPrice { get; set; }

    /// <summary>
    /// 当前在库批次列表（Status=1 且 Quantity&gt;0），供盘亏时选择扣减批次
    /// </summary>
    public List<InventoryCheckBatchOptionDto> Batches { get; set; } = new();
}

/// <summary>
/// 盘点专用批次选项（盘亏时供操作员选择扣减批次）
/// </summary>
public class InventoryCheckBatchOptionDto
{
    public long Id { get; set; }

    /// <summary>
    /// 批次号
    /// </summary>
    public string BatchNo { get; set; } = string.Empty;

    /// <summary>
    /// 当前批次库存数量
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// 批次单价（用于 FIFO 成本计算）
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// 过期日期
    /// </summary>
    public DateTime? ExpirationDate { get; set; }

    /// <summary>
    /// 批次创建时间（用于 FIFO 排序展示）
    /// </summary>
    public DateTime CreatedTime { get; set; }
}

/// <summary>
/// 盘盈批次查询结果（按批次号查当前商品在当前门店的全部历史批次，含已扣完）
/// 用于盘盈录入时校验批次号存在性并带出批次属性
/// </summary>
public class InventoryCheckBatchLookupDto
{
    public long Id { get; set; }

    /// <summary>
    /// 批次号
    /// </summary>
    public string BatchNo { get; set; } = string.Empty;

    /// <summary>
    /// 当前批次库存数量
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// 批次单价
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// 生产日期
    /// </summary>
    public DateTime? ProductionDate { get; set; }

    /// <summary>
    /// 保质期天数
    /// </summary>
    public int? ShelfLifeDays { get; set; }

    /// <summary>
    /// 过期日期
    /// </summary>
    public DateTime? ExpirationDate { get; set; }

    /// <summary>
    /// 状态（1:在库 2:已用完 3:已过期）
    /// </summary>
    public int Status { get; set; }
}
