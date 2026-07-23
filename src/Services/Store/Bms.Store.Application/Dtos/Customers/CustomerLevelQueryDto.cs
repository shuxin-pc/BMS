using Bms.Store.Application.Dtos;

namespace Bms.Store.Application.Dtos.Customers;

/// <summary>
/// 客户等级分页查询参数
/// </summary>
public class CustomerLevelQueryDto : PagedRequestDto
{
    /// <summary>
    /// 等级名称（模糊匹配）
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// 等级编码（模糊匹配）
    /// </summary>
    public string? Code { get; set; }
}
