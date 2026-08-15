using Bms.Store.Application.Dtos;

namespace Bms.Store.Application.Dtos.Customers;

public class ServiceComparisonPhotoQueryDto : PagedRequestDto
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
    /// 服务项目（模糊匹配）
    /// </summary>
    public string? ServiceItem { get; set; }
}
