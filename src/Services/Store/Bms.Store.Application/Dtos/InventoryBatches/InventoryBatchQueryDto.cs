using Bms.Store.Application.Dtos;

namespace Bms.Store.Application.Dtos.InventoryBatches;

/// <summary>
/// 库存批次分页查询参数
/// </summary>
public class InventoryBatchQueryDto : PagedRequestDto
{
    /// <summary>
    /// 商品ID
    /// </summary>
    public long? ProductId { get; set; }

    /// <summary>
    /// 批次号（模糊匹配）
    /// </summary>
    public string? BatchNo { get; set; }

    /// <summary>
    /// 状态（1:在库 2:已用完 3:已过期）
    /// </summary>
    public int? Status { get; set; }
}
