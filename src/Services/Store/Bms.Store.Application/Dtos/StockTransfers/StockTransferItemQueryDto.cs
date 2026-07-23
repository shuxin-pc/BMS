using Bms.Store.Application.Dtos;

namespace Bms.Store.Application.Dtos.StockTransfers;

/// <summary>
/// 库存调拨单明细分页查询参数
/// </summary>
public class StockTransferItemQueryDto : PagedRequestDto
{
    /// <summary>
    /// 调拨单ID
    /// </summary>
    public long? StockTransferId { get; set; }

    /// <summary>
    /// 商品ID
    /// </summary>
    public long? ProductId { get; set; }

    /// <summary>
    /// 批次号（模糊匹配）
    /// </summary>
    public string? BatchNo { get; set; }
}
