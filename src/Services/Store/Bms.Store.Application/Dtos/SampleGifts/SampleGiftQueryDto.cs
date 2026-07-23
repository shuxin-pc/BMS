using Bms.Store.Application.Dtos;

namespace Bms.Store.Application.Dtos.SampleGifts;

/// <summary>
/// 样品/赠品档案分页查询参数
/// 样品/赠品即 Product 表中 Type=4（样品）或 Type=5（赠品）的记录
/// </summary>
public class SampleGiftQueryDto : PagedRequestDto
{
    /// <summary>
    /// 商品名称（模糊匹配）
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// 商品编码（模糊匹配）
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    /// 类型筛选：4-样品，5-赠品，null=全部
    /// </summary>
    public int? Type { get; set; }

    /// <summary>
    /// 商品状态：1-上架，2-下架，null=全部
    /// </summary>
    public int? Status { get; set; }
}
