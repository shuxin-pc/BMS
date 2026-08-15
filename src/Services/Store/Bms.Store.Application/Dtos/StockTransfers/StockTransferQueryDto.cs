using Bms.Store.Application.Dtos;

namespace Bms.Store.Application.Dtos.StockTransfers;

/// <summary>
/// 库存调拨单分页查询参数
/// </summary>
public class StockTransferQueryDto : PagedRequestDto
{
    /// <summary>
    /// 调拨单号（模糊匹配）
    /// </summary>
    public string? TransferNo { get; set; }

    /// <summary>
    /// 调出门店ID
    /// </summary>
    public long? FromStoreId { get; set; }

    /// <summary>
    /// 调入门店ID
    /// </summary>
    public long? ToStoreId { get; set; }

    /// <summary>
    /// 状态（1:待调出 2:已调出 3:已调入 4:已取消）
    /// </summary>
    public int? Status { get; set; }

    /// <summary>
    /// 调拨日期下界（含当日）
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// 调拨日期上界（含当日）
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// 商品类型筛选（1:实物商品 2:服务商品 3:耗材 4:样品 5:赠品）
    /// 返回明细中含该类型商品的调拨单
    /// </summary>
    public int? ProductType { get; set; }
}
