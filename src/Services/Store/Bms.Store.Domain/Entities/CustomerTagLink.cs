namespace Bms.Store.Domain.Entities;

/// <summary>
/// 客户-标签 关联表（多对多）
/// </summary>
public class CustomerTagLink : StoreEntity
{
    /// <summary>
    /// 客户 ID（关联 Customer.Id）
    /// </summary>
    public long CustomerId { get; set; }

    /// <summary>
    /// 标签 ID（关联 CustomerTag.Id）
    /// </summary>
    public long TagId { get; set; }

    /// <summary>
    /// 导航属性：客户
    /// </summary>
    public Customer? Customer { get; set; }

    /// <summary>
    /// 导航属性：标签
    /// </summary>
    public CustomerTag? Tag { get; set; }
}
