using Bms.Store.Application.Dtos;

namespace Bms.Store.Application.Dtos.Customers;

/// <summary>
/// 客户档案分页查询参数
/// </summary>
public class CustomerQueryDto : PagedRequestDto
{
    /// <summary>
    /// 客户姓名（模糊匹配）
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// 手机号（精确匹配）
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// 客户等级ID
    /// </summary>
    public long? LevelId { get; set; }
}
