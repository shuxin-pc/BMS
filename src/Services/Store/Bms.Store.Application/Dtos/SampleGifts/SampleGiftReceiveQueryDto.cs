using Bms.Store.Application.Dtos;

namespace Bms.Store.Application.Dtos.SampleGifts;

/// <summary>
/// 样品领用记录分页查询参数
/// </summary>
public class SampleGiftReceiveQueryDto : PagedRequestDto
{
    public long? ProductId { get; set; }
    public long? CustomerId { get; set; }

    /// <summary>
    /// 关联活动ID筛选
    /// </summary>
    public long? ActivityId { get; set; }

    /// <summary>
    /// 客户名称或手机号关键字（模糊匹配，OR 语义：命中姓名或手机号其一即满足）
    /// </summary>
    public string? Keyword { get; set; }
}
