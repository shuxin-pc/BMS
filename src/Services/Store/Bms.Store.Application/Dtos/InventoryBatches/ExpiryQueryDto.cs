using Bms.Store.Application.Dtos;

namespace Bms.Store.Application.Dtos.InventoryBatches;

/// <summary>
/// 效期查询参数
/// </summary>
public class ExpiryQueryDto : PagedRequestDto
{
    /// <summary>
    /// 商品名称（模糊匹配）
    /// </summary>
    public string? ProductName { get; set; }

    /// <summary>
    /// 效期状态（normal:正常 expiring:即将过期 expired:已过期）
    /// </summary>
    public string? Status { get; set; }
}
