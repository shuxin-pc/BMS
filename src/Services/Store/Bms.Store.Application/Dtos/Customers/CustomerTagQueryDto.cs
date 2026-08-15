using Bms.Store.Application.Dtos;

namespace Bms.Store.Application.Dtos.Customers;

/// <summary>
/// 客户标签分页查询参数
/// </summary>
public class CustomerTagQueryDto : PagedRequestDto
{
    /// <summary>
    /// 标签名称（模糊匹配）
    /// </summary>
    public string? Name { get; set; }
}
