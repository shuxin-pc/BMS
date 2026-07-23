namespace Bms.Store.Domain.Entities;

/// <summary>
/// 服务前后对比照片
/// 关联订单ID，记录客户服务前后的照片对比
/// </summary>
public class ServiceComparisonPhoto : StoreBusinessEntityBase
{
    /// <summary>
    /// 客户ID
    /// </summary>
    public long CustomerId { get; set; }

    /// <summary>
    /// 关联订单ID（必填，对比照片必须关联到具体服务订单）
    /// </summary>
    public long OrderId { get; set; }

    /// <summary>
    /// 服务项目
    /// </summary>
    public string? ServiceItem { get; set; }

    /// <summary>
    /// 拍照日期
    /// </summary>
    public DateTime PhotoDate { get; set; }

    /// <summary>
    /// 照片类型（1:服务前 2:服务后）
    /// </summary>
    public int PhotoType { get; set; }

    /// <summary>
    /// 照片URL
    /// </summary>
    public string PhotoUrl { get; set; } = string.Empty;

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 导航属性：客户
    /// </summary>
    public Customer? Customer { get; set; }
}
