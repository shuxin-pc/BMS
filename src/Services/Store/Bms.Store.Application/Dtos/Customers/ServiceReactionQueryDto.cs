using Bms.Store.Application.Dtos;

namespace Bms.Store.Application.Dtos.Customers;

public class ServiceReactionQueryDto : PagedRequestDto
{
    public long? CustomerId { get; set; }
    /// <summary>
    /// 客户名称或手机号关键字（模糊匹配，OR 语义：命中姓名或手机号其一即满足）
    /// </summary>
    public string? Keyword { get; set; }
    /// <summary>
    /// 反应日期起（含）
    /// </summary>
    public DateTime? StartDate { get; set; }
    /// <summary>
    /// 反应日期止（含）
    /// </summary>
    public DateTime? EndDate { get; set; }
    /// <summary>
    /// 严重程度：1-轻微，2-中等，3-严重
    /// </summary>
    public int? Severity { get; set; }
}
