namespace Bms.Store.Application.Dtos.Dashboard;

/// <summary>
/// 营收构成项（饼图数据）
/// </summary>
public class RevenueCompositionItemDto
{
    /// <summary>
    /// 分类名称（零售/服务/疗程卡等）
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 金额
    /// </summary>
    public decimal Value { get; set; }
}
