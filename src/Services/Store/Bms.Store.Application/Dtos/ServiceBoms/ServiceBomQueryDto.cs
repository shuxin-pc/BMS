using Bms.Store.Application.Dtos;

namespace Bms.Store.Application.Dtos.ServiceBoms;

/// <summary>
/// 服务BOM分页查询参数
/// </summary>
public class ServiceBomQueryDto : PagedRequestDto
{
    public long? ServiceProductId { get; set; }
    public long? ConsumableProductId { get; set; }
}
