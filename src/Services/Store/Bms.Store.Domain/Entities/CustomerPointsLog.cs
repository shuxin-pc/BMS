namespace Bms.Store.Domain.Entities;

/// <summary>
/// 客户积分流水
/// </summary>
public class CustomerPointsLog : StoreBusinessEntityBase
{
    /// <summary>
    /// 客户ID
    /// </summary>
    public long CustomerId { get; set; }

    /// <summary>
    /// 积分类型（参见 <see cref="Bms.Store.Domain.Constants.CustomerPointsLogType"/>）
    /// 1:消费获得 2:积分抵扣 3:退款扣减 5:充值获得 6:疗程卡购买获得 7:过期清零 8:手动调整
    /// </summary>
    public int Type { get; set; }

    /// <summary>
    /// 积分变化（正数增加，负数减少）
    /// </summary>
    public int Points { get; set; }

    /// <summary>
    /// 变化前积分
    /// </summary>
    public int BeforePoints { get; set; }

    /// <summary>
    /// 变化后积分
    /// </summary>
    public int AfterPoints { get; set; }

    /// <summary>
    /// 关联订单ID
    /// </summary>
    public long? OrderId { get; set; }

    /// <summary>
    /// 操作人ID
    /// </summary>
    public long? OperatorId { get; set; }

    /// <summary>
    /// 积分有效期截止日期
    /// </summary>
    public DateTime? ExpireDate { get; set; }

    /// <summary>
    /// 是否已过期清零（仅对发放类记录有意义，true 表示已由过期任务清零）
    /// </summary>
    public bool IsExpired { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 导航属性：客户
    /// </summary>
    public Customer? Customer { get; set; }
}
