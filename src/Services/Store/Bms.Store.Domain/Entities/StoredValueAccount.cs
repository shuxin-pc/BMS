namespace Bms.Store.Domain.Entities;

/// <summary>
/// 储值账户（租户内跨店通用，开户门店永久归属，遵循原则4跨店权益共享）
/// </summary>
public class StoredValueAccount : StoreEntity
{
    /// <summary>
    /// 客户ID
    /// </summary>
    public long CustomerId { get; set; }

    /// <summary>
    /// 当前余额（总余额 = 实收余额 + 赠送余额，冗余字段便于查询）
    /// </summary>
    public decimal Balance { get; set; }

    /// <summary>
    /// 实收余额（客户实际支付充值金额对应的余额）
    /// 消费时先扣实收余额，退款时仅退还剩余实收余额
    /// </summary>
    public decimal RealBalance { get; set; }

    /// <summary>
    /// 赠送余额（充值时赠送的金额对应的余额）
    /// 消费时实收余额不足后扣赠送余额，赠送余额一律不退
    /// </summary>
    public decimal GiftBalance { get; set; }

    /// <summary>
    /// 累计充值金额
    /// </summary>
    public decimal TotalRecharge { get; set; }

    /// <summary>
    /// 累计赠送金额
    /// </summary>
    public decimal TotalGift { get; set; }

    /// <summary>
    /// 累计消费金额
    /// </summary>
    public decimal TotalConsume { get; set; }

    /// <summary>
    /// 导航属性：客户
    /// </summary>
    public Customer? Customer { get; set; }
}
