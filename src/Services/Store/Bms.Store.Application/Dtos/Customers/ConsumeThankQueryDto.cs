using Bms.Store.Application.Dtos;

namespace Bms.Store.Application.Dtos.Customers;

/// <summary>
/// 消费感谢分页查询参数
/// </summary>
public class ConsumeThankQueryDto : PagedRequestDto
{
    /// <summary>
    /// 客户姓名（模糊匹配）
    /// </summary>
    public string? CustomerName { get; set; }

    /// <summary>
    /// 手机号（模糊匹配）
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// 感谢状态：1=待感谢 2=已感谢
    /// </summary>
    public int? ThankStatus { get; set; }
}
