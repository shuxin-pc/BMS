namespace Bms.Store.Application.Dtos.Products;

/// <summary>
/// 商品主档轻量选项 DTO（用于下拉选择场景）
/// </summary>
public class ProductMasterOptionDto
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Type { get; set; }
    public string? Unit { get; set; }
}
