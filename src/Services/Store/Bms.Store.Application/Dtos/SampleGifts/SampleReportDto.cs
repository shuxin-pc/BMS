namespace Bms.Store.Application.Dtos.SampleGifts;

/// <summary>
/// 样品统计报表（按商品维度，原 GetReportListAsync 接口返回类型）
/// </summary>
public class SampleReportDto
{
    /// <summary>
    /// 档案ID
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 类型（4:样品 5:赠品）
    /// </summary>
    public int Type { get; set; }

    /// <summary>
    /// 领用数量
    /// </summary>
    public decimal ReceiveCount { get; set; }

    /// <summary>
    /// 出库数量
    /// </summary>
    public decimal OutboundCount { get; set; }

    /// <summary>
    /// 合计发出
    /// </summary>
    public decimal TotalIssued { get; set; }

    /// <summary>
    /// 当前库存
    /// </summary>
    public decimal CurrentStock { get; set; }

    /// <summary>
    /// 领用占比（%）
    /// </summary>
    public decimal ReceiveRatio { get; set; }
}

/// <summary>
/// 样品/赠品按活动维度统计报表（P-SG-04）
/// R5：数据源从 SampleGiftOut 迁移到 InventoryLogs（SourceType 9=样品领用出库/10=赠品活动出库）
/// 聚合维度：ActivityId + ProductId
/// </summary>
public class SampleActivityReportDto
{
    /// <summary>
    /// 活动ID（null 表示未关联活动的赠品出库）
    /// </summary>
    public long? ActivityId { get; set; }

    /// <summary>
    /// 商品ID
    /// </summary>
    public long ProductId { get; set; }

    /// <summary>
    /// 商品名称
    /// </summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// 出库数量合计
    /// </summary>
    public decimal OutboundCount { get; set; }

    /// <summary>
    /// 出库金额合计（按 CostPrice 计算）
    /// </summary>
    public decimal TotalValue { get; set; }

    /// <summary>
    /// 出库记录数
    /// </summary>
    public int OutboundRecords { get; set; }

    /// <summary>
    /// 最近一次出库时间
    /// </summary>
    public DateTime? LastOutTime { get; set; }
}

/// <summary>
/// 样品/赠品按客户维度统计报表（P-SG-04）
/// 数据源：SampleGiftReceive（仅样品领用有 CustomerId）
/// 聚合维度：CustomerId
/// </summary>
public class SampleCustomerReportDto
{
    /// <summary>
    /// 客户ID
    /// </summary>
    public long CustomerId { get; set; }

    /// <summary>
    /// 客户名称
    /// </summary>
    public string? CustomerName { get; set; }

    /// <summary>
    /// 客户手机号
    /// </summary>
    public string? CustomerPhone { get; set; }

    /// <summary>
    /// 领用次数
    /// </summary>
    public int ReceiveTimes { get; set; }

    /// <summary>
    /// 领用数量合计
    /// </summary>
    public decimal ReceiveCount { get; set; }

    /// <summary>
    /// 最近一次领用时间
    /// </summary>
    public DateTime? LastReceiveTime { get; set; }
}
