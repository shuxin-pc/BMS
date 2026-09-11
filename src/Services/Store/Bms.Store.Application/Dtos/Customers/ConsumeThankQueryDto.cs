using Bms.Store.Application.Dtos;

namespace Bms.Store.Application.Dtos.Customers;

/// <summary>
/// 消费感谢分页查询参数
/// </summary>
public class ConsumeThankQueryDto : PagedRequestDto
{
    /// <summary>
    /// 客户名称或手机号关键字（模糊匹配，OR 语义：命中姓名或手机号其一即满足）
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// 感谢状态：1=待感谢 2=已感谢
    /// </summary>
    public int? ThankStatus { get; set; }
}
