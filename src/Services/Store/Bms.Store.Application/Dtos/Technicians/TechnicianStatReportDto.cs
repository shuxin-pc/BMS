namespace Bms.Store.Application.Dtos.Technicians;

/// <summary>
/// 技师业绩统计报表响应 DTO（外层包装）
/// 对应需求 B4.5：纯平台技师门店需隐藏业绩报表，外层携带 IsPurePlatformStore 标识供前端判断
/// </summary>
public class TechnicianStatReportDto
{
    /// <summary>
    /// 是否纯平台技师门店（无自有技师）
    /// true 时表示本门店所有技师均为平台技师，业绩由平台统一统计，门店端不展示
    /// </summary>
    public bool IsPurePlatformStore { get; set; }

    /// <summary>
    /// 提示信息（纯平台技师门店时返回，前端用于显示提示文案）
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// 当前页数据列表（纯平台技师门店时为空）
    /// </summary>
    public List<TechnicianStatisticReportDto> Items { get; set; } = new();

    /// <summary>
    /// 总记录数
    /// </summary>
    public int Total { get; set; }

    /// <summary>
    /// 当前页码
    /// </summary>
    public int PageIndex { get; set; }

    /// <summary>
    /// 每页条数
    /// </summary>
    public int PageSize { get; set; }
}
