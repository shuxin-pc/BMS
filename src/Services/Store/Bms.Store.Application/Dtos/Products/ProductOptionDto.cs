namespace Bms.Store.Application.Dtos.Products;

/// <summary>
/// 商品轻量选项 DTO（用于下拉选择场景，不分页）
/// </summary>
public class ProductOptionDto
{
    /// <summary>商品ID</summary>
    public long Id { get; set; }

    /// <summary>商品名称</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>商品编码</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>单位</summary>
    public string? Unit { get; set; }

    /// <summary>
    /// 商品类型（1:实物商品 2:服务商品 3:耗材 4:样品 5:赠品）
    /// 用于前端按场景过滤（如出库需排除服务商品）
    /// </summary>
    public int Type { get; set; }
}
