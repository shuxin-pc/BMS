using Bms.Store.Application.Dtos;

namespace Bms.Store.Application.Dtos.SampleGiftTransfers;

/// <summary>
/// 样品赠品调拨单分页查询参数
/// </summary>
public class SampleGiftTransferQueryDto : PagedRequestDto
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
    /// 状态（1:待调出 3:已调入 4:已取消）
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
    /// 商品类型筛选：null=全部 / 4=样品 / 5=赠品
    /// </summary>
    public int? ProductType { get; set; }
}
