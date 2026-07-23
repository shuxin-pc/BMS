using Bms.Store.Application.Dtos;

namespace Bms.Store.Application.Dtos.Statistics;

/// <summary>
/// 商品销售统计分页查询参数
/// </summary>
public class ProductSalesStatQueryDto : PagedRequestDto
{
    /// <summary>
    /// 统计月份（yyyy-MM）
    /// </summary>
    public string? StatMonth { get; set; }

    /// <summary>
    /// 商品ID
    /// </summary>
    public long? ProductId { get; set; }

    /// <summary>
    /// 商品类型（1:零售 2:服务 3:耗材）
    /// </summary>
    public int? ProductType { get; set; }
}
