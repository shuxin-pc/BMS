using Bms.Store.Application.Dtos;

namespace Bms.Store.Application.Dtos.Products;

/// <summary>
/// 商品主档分页查询参数（租户级，无 StoreId 过滤）
/// </summary>
public class ProductMasterQueryDto : PagedRequestDto
{
    /// <summary>商品名称（模糊匹配）</summary>
    public string? Name { get; set; }

    /// <summary>商品编码（模糊匹配）</summary>
    public string? Code { get; set; }

    /// <summary>商品类型（1:实物 2:服务 3:耗材 4:样品 5:赠品）</summary>
    public int? Type { get; set; }

    /// <summary>分类ID（含子孙分类）</summary>
    public long? CategoryId { get; set; }
}
