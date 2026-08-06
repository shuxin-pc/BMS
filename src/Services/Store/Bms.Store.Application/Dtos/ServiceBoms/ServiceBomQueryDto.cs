using Bms.Store.Application.Dtos;

namespace Bms.Store.Application.Dtos.ServiceBoms;

/// <summary>
/// 服务BOM分页查询参数
/// </summary>
public class ServiceBomQueryDto : PagedRequestDto
{
    public long? ServiceProductId { get; set; }
    public long? ConsumableProductId { get; set; }

    /// <summary>服务项目名称（模糊匹配）</summary>
    public string? ServiceProductName { get; set; }

    /// <summary>耗材商品名称（模糊匹配）</summary>
    public string? ConsumableProductName { get; set; }
}
