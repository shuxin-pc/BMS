namespace Bms.Store.Application.Dtos.Customers;

/// <summary>
/// 客户身体数据趋势 DTO
/// 按时间序列返回身体数据，支持多维度趋势分析
/// </summary>
public class BodyDataTrendDto
{
    /// <summary>
    /// 客户ID
    /// </summary>
    public long CustomerId { get; set; }

    /// <summary>
    /// 起始日期
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// 结束日期
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// 时间序列（按记录日期升序）
    /// </summary>
    public List<DateTime> Dates { get; set; } = new();

    /// <summary>
    /// 体重序列(kg)
    /// </summary>
    public List<decimal?> WeightSeries { get; set; } = new();

    /// <summary>
    /// 体脂率序列(%)
    /// </summary>
    public List<decimal?> BodyFatSeries { get; set; } = new();

    /// <summary>
    /// 胸围序列(cm)
    /// </summary>
    public List<decimal?> BustSeries { get; set; } = new();

    /// <summary>
    /// 腰围序列(cm)
    /// </summary>
    public List<decimal?> WaistSeries { get; set; } = new();

    /// <summary>
    /// 臀围序列(cm)
    /// </summary>
    public List<decimal?> HipSeries { get; set; } = new();

    /// <summary>
    /// 体重变化量（末值-首值）
    /// </summary>
    public decimal? WeightChange { get; set; }

    /// <summary>
    /// 体脂率变化量（末值-首值）
    /// </summary>
    public decimal? BodyFatChange { get; set; }

    /// <summary>
    /// 胸围变化量（末值-首值）
    /// </summary>
    public decimal? BustChange { get; set; }

    /// <summary>
    /// 腰围变化量（末值-首值）
    /// </summary>
    public decimal? WaistChange { get; set; }

    /// <summary>
    /// 臀围变化量（末值-首值）
    /// </summary>
    public decimal? HipChange { get; set; }

    /// <summary>
    /// 时间范围总天数
    /// </summary>
    public int DaysCount { get; set; }

    /// <summary>
    /// 实际记录条数
    /// </summary>
    public int RecordsCount { get; set; }
}

/// <summary>
/// 客户身体数据对比 DTO
/// 对比两个时间点的身体数据差异
/// </summary>
public class BodyDataComparisonDto
{
    /// <summary>
    /// 客户ID
    /// </summary>
    public long CustomerId { get; set; }

    /// <summary>
    /// 起始记录
    /// </summary>
    public BodyDataRecordDto? StartRecord { get; set; }

    /// <summary>
    /// 结束记录
    /// </summary>
    public BodyDataRecordDto? EndRecord { get; set; }

    /// <summary>
    /// 体重变化量（末值-首值）
    /// </summary>
    public decimal? WeightChange { get; set; }

    /// <summary>
    /// 体脂率变化量（末值-首值）
    /// </summary>
    public decimal? BodyFatChange { get; set; }

    /// <summary>
    /// 胸围变化量（末值-首值）
    /// </summary>
    public decimal? BustChange { get; set; }

    /// <summary>
    /// 腰围变化量（末值-首值）
    /// </summary>
    public decimal? WaistChange { get; set; }

    /// <summary>
    /// 臀围变化量（末值-首值）
    /// </summary>
    public decimal? HipChange { get; set; }

    /// <summary>
    /// 两个时间点间隔天数
    /// </summary>
    public int DaysBetween { get; set; }
}
