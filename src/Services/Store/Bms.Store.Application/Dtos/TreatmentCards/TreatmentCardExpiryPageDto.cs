namespace Bms.Store.Application.Dtos.TreatmentCards;

/// <summary>
/// 项目卡到期提醒分页响应（含全量预警级别统计）
/// 统计口径：基于当前搜索条件（租户/门店/客户名称）下 30 天内到期或已过期的全量记录，
/// 不受预警级别筛选与分页影响，保证统计卡片与列表分页口径一致
/// </summary>
public class TreatmentCardExpiryPageDto
{
    /// <summary>
    /// 数据列表
    /// </summary>
    public List<TreatmentCardExpiryDto> List { get; set; } = new();

    /// <summary>
    /// 总数（受预警级别筛选影响）
    /// </summary>
    public int Total { get; set; }

    /// <summary>
    /// 当前页码
    /// </summary>
    public int PageIndex { get; set; }

    /// <summary>
    /// 每页数量
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// 即将到期数量（全量，不受预警级别筛选与分页影响）
    /// </summary>
    public int ExpiringCount { get; set; }

    /// <summary>
    /// 已到期数量（全量，不受预警级别筛选与分页影响）
    /// </summary>
    public int ExpiredCount { get; set; }
}
