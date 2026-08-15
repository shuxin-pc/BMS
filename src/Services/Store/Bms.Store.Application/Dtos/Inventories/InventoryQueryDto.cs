using Bms.Store.Application.Dtos;

namespace Bms.Store.Application.Dtos.Inventories;

/// <summary>
/// 库存分页查询参数
/// </summary>
public class InventoryQueryDto : PagedRequestDto
{
    public long? ProductId { get; set; }

    /// <summary>
    /// 商品分类ID（按 Product.CategoryId 筛选）
    /// </summary>
    public long? CategoryId { get; set; }

    /// <summary>
    /// 商品名称（模糊匹配 ProductMaster.Name）
    /// </summary>
    public string? ProductName { get; set; }

    /// <summary>
    /// 商品类型（1:实物商品 2:服务商品 3:耗材 4:样品 5:赠品）
    /// </summary>
    public int? ProductType { get; set; }

    /// <summary>
    /// 库存状态（1:充足 2:偏低 3:不足 4:积压），在内存中按阈值计算后过滤
    /// </summary>
    public int? InventoryStatus { get; set; }
}
