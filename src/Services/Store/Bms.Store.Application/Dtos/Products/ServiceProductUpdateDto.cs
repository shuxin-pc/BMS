namespace Bms.Store.Application.Dtos.Products;

/// <summary>
/// 更新服务商品子表输入 DTO
/// </summary>
public class ServiceProductUpdateDto : ServiceProductCreateDto
{
    public long Id { get; set; }
}
