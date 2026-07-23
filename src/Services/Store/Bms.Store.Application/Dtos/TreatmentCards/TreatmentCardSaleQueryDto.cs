using Bms.Store.Application.Dtos;

namespace Bms.Store.Application.Dtos.TreatmentCards;

/// <summary>
/// 疗程卡销售记录分页查询 DTO
/// </summary>
public class TreatmentCardSaleQueryDto : PagedRequestDto
{
    /// <summary>
    /// 客户ID
    /// </summary>
    public long? CustomerId { get; set; }

    /// <summary>
    /// 疗程卡ID
    /// </summary>
    public long? CardId { get; set; }

    /// <summary>
    /// 状态筛选（1:有效 2:已用完 3:已过期）
    /// </summary>
    public int? Status { get; set; }

    /// <summary>
    /// 客户名称（模糊匹配，join Customer 表查询）
    /// </summary>
    public string? CustomerName { get; set; }

    /// <summary>
    /// 手机号（模糊匹配，join Customer 表查询）
    /// </summary>
    public string? Phone { get; set; }
}
