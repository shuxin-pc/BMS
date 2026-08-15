using Bms.Store.Application.Dtos;

namespace Bms.Store.Application.Dtos.Customers;

/// <summary>
/// 客户美容档案分页查询参数
/// </summary>
public class CustomerBeautyProfileQueryDto : PagedRequestDto
{
    /// <summary>
    /// 客户ID
    /// </summary>
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
    /// 肤质类型
    /// </summary>
    public string? SkinType { get; set; }
}
