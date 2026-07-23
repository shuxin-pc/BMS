namespace Bms.Store.Domain.Entities;

/// <summary>
/// 消费记录
/// </summary>
public class ConsumeLog : StoreBusinessEntityBase
{
    /// <summary>
    /// 客户ID
    /// </summary>
    public long CustomerId { get; set; }

    /// <summary>
    /// 订单ID
    /// </summary>
    public long OrderId { get; set; }

    /// <summary>
    /// 消费金额
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// 获得积分
    /// </summary>
    public int Points { get; set; }

    /// <summary>
    /// 消费时间
    /// </summary>
    public DateTime ConsumeTime { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 导航属性：客户
    /// </summary>
    public Customer? Customer { get; set; }
}
