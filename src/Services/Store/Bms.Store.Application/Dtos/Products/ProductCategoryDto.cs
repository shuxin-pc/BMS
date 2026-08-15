namespace Bms.Store.Application.Dtos.Products;

/// <summary>
/// 商品分类输出 DTO（对齐前端 ProductCategory 契约，含树形 children）
/// </summary>
public class ProductCategoryDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public long ParentId { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<ProductCategoryDto>? Children { get; set; }
}

/// <summary>
/// 创建商品分类输入 DTO
/// </summary>
public class ProductCategoryCreateDto
{
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public long ParentId { get; set; }
}

/// <summary>
/// 更新商品分类输入 DTO
/// </summary>
public class ProductCategoryUpdateDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public long ParentId { get; set; }
}
