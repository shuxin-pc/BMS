namespace Bms.Store.Application.Dtos.Products;

/// <summary>
/// 更新商品输入 DTO
/// </summary>
public class ProductUpdateDto : ProductCreateDto
{
    public long Id { get; set; }
}
