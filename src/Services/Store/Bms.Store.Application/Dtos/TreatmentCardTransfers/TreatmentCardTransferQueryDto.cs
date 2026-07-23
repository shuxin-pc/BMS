using Bms.Store.Application.Dtos;

namespace Bms.Store.Application.Dtos.TreatmentCardTransfers;

/// <summary>
/// 疗程卡转让分页查询 DTO
/// </summary>
public class TreatmentCardTransferQueryDto : PagedRequestDto
{
    /// <summary>
    /// 疗程卡销售记录ID
    /// </summary>
    public long? CardSaleId { get; set; }

    /// <summary>
    /// 原客户ID
    /// </summary>
    public long? FromCustomerId { get; set; }

    /// <summary>
    /// 新客户ID
    /// </summary>
    public long? ToCustomerId { get; set; }

    /// <summary>
    /// 状态筛选（1:已转让）
    /// </summary>
    public int? Status { get; set; }
}
