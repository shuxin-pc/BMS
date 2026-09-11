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
    /// 手机号（模糊匹配）
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// 客户名称或手机号关键字（模糊匹配，OR 语义：命中姓名或手机号其一即满足）
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// 客户等级ID
    /// </summary>
    public long? LevelId { get; set; }

    /// <summary>
    /// 客户标签ID（按标签筛选关联客户）
    /// </summary>
    public long? TagId { get; set; }

    /// <summary>
    /// 性别（0:未知 1:男 2:女）
    /// </summary>
    public int? Gender { get; set; }
}
