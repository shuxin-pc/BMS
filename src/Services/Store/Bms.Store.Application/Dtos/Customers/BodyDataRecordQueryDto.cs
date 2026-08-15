using Bms.Store.Application.Dtos;

namespace Bms.Store.Application.Dtos.Customers;

public class BodyDataRecordQueryDto : PagedRequestDto
{
    public long? CustomerId { get; set; }
    /// <summary>
    /// 客户姓名（模糊匹配）
    /// </summary>
    public string? CustomerName { get; set; }
    /// <summary>
    /// 客户手机号（模糊匹配）
    /// </summary>
    public string? CustomerPhone { get; set; }
    /// <summary>
    /// 记录日期起始（含）
    /// </summary>
    public DateTime? StartDate { get; set; }
    /// <summary>
    /// 记录日期结束（含）
    /// </summary>
    public DateTime? EndDate { get; set; }
}
