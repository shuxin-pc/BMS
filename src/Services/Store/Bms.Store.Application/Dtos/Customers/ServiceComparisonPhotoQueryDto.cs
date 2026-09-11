using Bms.Store.Application.Dtos;

namespace Bms.Store.Application.Dtos.Customers;

public class ServiceComparisonPhotoQueryDto : PagedRequestDto
{
    public long? CustomerId { get; set; }
    /// <summary>
    /// 客户名称或手机号关键字（模糊匹配，OR 语义：命中姓名或手机号其一即满足）
    /// </summary>
    public string? Keyword { get; set; }
    /// <summary>
    /// 服务项目（模糊匹配）
    /// </summary>
    public string? ServiceItem { get; set; }
}
