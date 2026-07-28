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
}
