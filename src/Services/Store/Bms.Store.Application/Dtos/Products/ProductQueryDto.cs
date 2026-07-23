using Bms.Store.Application.Dtos;

namespace Bms.Store.Application.Dtos.Products;

/// <summary>
/// 商品分页查询参数
/// </summary>
public class ProductQueryDto : PagedRequestDto
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
    /// 分类ID
    /// </summary>
    public long? CategoryId { get; set; }

    /// <summary>
    /// 供应商ID
    /// </summary>
    public long? SupplierId { get; set; }

    /// <summary>
    /// 商品状态
    /// </summary>
    public int? Status { get; set; }

    /// <summary>
    /// 商品类型（1:实物 2:服务 3:耗材 4:样品 5:赠品）
    /// </summary>
    public int? Type { get; set; }
}
