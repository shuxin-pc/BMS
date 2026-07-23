namespace Bms.Store.Application.Dtos.DailySettlements;

/// <summary>
/// 批量补日结请求
/// </summary>
public class BatchSummarizeRequestDto
{
    /// <summary>
    /// 起始日期（含）
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// 结束日期（含）
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// 是否创建后自动确认（true=创建并自动确认，false=仅创建待确认）
    /// </summary>
    public bool AutoConfirm { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}

/// <summary>
/// 批量补日结结果
/// </summary>
public class BatchSummarizeResultDto
{
    /// <summary>
    /// 成功创建的日期列表
    /// </summary>
    public List<DateTime> Success { get; set; } = new();

    /// <summary>
    /// 跳过的日期列表（已存在日结记录）
    /// </summary>
    public List<DateTime> Skipped { get; set; } = new();

    /// <summary>
    /// 失败的日期列表及原因
    /// </summary>
    public List<BatchSummarizeFailedItem> Failed { get; set; } = new();
}

/// <summary>
/// 批量补日结失败项
/// </summary>
public class BatchSummarizeFailedItem
{
    /// <summary>
    /// 失败日期
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// 失败原因
    /// </summary>
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// 历史漏日结回补结果
/// </summary>
public class BackfillResultDto
{
    /// <summary>
    /// 总天数（含跳过）
    /// </summary>
    public int TotalDays { get; set; }

    /// <summary>
    /// 新建记录数
    /// </summary>
    public int CreatedCount { get; set; }

    /// <summary>
    /// 跳过记录数（已存在）
    /// </summary>
    public int SkippedCount { get; set; }
}
