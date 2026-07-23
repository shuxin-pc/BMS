namespace Bms.Store.Application.Dtos.Dashboard;

/// <summary>
/// 热门商品 TOP N 查询参数
/// </summary>
public class TopProductsQueryDto
{
    /// <summary>
    /// 年份
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// 月份（1-12）
    /// </summary>
    public int Month { get; set; }

    /// <summary>
    /// 商品类型筛选（多值，1:零售 2:服务 3:耗材 4:疗程卡），不传则查全部
    /// </summary>
    public List<int>? ProductTypes { get; set; }

    /// <summary>
    /// 返回前 N 条，默认 5
    /// </summary>
    public int Top { get; set; } = 5;

    /// <summary>
    /// 排序字段（amount:按金额 count:按次数），默认 amount
    /// </summary>
    public string? SortBy { get; set; } = "amount";
}
