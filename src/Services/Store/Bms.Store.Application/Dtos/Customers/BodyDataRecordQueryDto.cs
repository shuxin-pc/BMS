using Bms.Store.Application.Dtos;

namespace Bms.Store.Application.Dtos.Customers;

public class BodyDataRecordQueryDto : PagedRequestDto
{
    public long? CustomerId { get; set; }
    /// <summary>
    /// 客户名称或手机号关键字（模糊匹配，OR 语义：命中姓名或手机号其一即满足）
    /// </summary>
    public string? Keyword { get; set; }
    /// <summary>
    /// 记录日期起始（含）
    /// </summary>
    public DateTime? StartDate { get; set; }
    /// <summary>
    /// 记录日期结束（含）
    /// </summary>
    public DateTime? EndDate { get; set; }
}
