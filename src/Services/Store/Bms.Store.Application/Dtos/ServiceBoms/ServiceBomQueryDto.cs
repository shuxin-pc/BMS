using Bms.Store.Application.Dtos;

namespace Bms.Store.Application.Dtos.ServiceBoms;

/// <summary>
/// 服务BOM分页查询参数
/// </summary>
public class ServiceBomQueryDto : PagedRequestDto
{
    public long? ServiceProductId { get; set; }
    public long? ConsumableProductId { get; set; }

    /// <summary>
    /// 门店商品档案ID（BOM 服务端关联服务项目档案，需经商品主档桥接：Products.MasterId == ServiceProducts.MasterId）
    /// 供快速开单等服务商品场景按商品档案反查绑定耗材
    /// </summary>
    public long? ProductId { get; set; }

    /// <summary>服务项目名称（模糊匹配）</summary>
    public string? ServiceProductName { get; set; }

    /// <summary>耗材商品名称（模糊匹配）</summary>
    public string? ConsumableProductName { get; set; }
}
