using Bms.Store.Application.Dtos;

namespace Bms.Store.Application.Dtos.Products;

/// <summary>
/// 服务商品子表分页查询参数
/// </summary>
public class ServiceProductQueryDto : PagedRequestDto
{
    public long? ProductId { get; set; }
}
