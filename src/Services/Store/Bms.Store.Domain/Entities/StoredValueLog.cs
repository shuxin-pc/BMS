namespace Bms.Store.Domain.Entities;

/// <summary>
/// 储值流水
/// </summary>
public class StoredValueLog : StoreBusinessEntityBase
{
    /// <summary>
    /// 客户ID
    /// </summary>
    public long CustomerId { get; set; }

    /// <summary>
    /// 流水类型（1:充值 2:消费 3:退款 4:调整）
    /// </summary>
    public int Type { get; set; }

    /// <summary>
    /// 金额变化（正数增加，负数减少）
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// 实收金额（充值时实际支付的金额）
    /// </summary>
    public decimal RealAmount { get; set; }

    /// <summary>
    /// 赠送金额（充值时赠送的金额）
    /// </summary>
    public decimal GiftAmount { get; set; }

    /// <summary>
    /// 变化前余额
    /// </summary>
    public decimal BeforeBalance { get; set; }

    /// <summary>
    /// 变化后余额
    /// </summary>
    public decimal AfterBalance { get; set; }

    /// <summary>
    /// 实收余额变动（正数增加，负数减少）
    /// </summary>
    public decimal RealBalanceChange { get; set; }

    /// <summary>
    /// 赠送余额变动（正数增加，负数减少）
    /// </summary>
    public decimal GiftBalanceChange { get; set; }

    /// <summary>
    /// 变化前实收余额
    /// </summary>
    public decimal BeforeRealBalance { get; set; }

    /// <summary>
    /// 变化后实收余额
    /// </summary>
    public decimal AfterRealBalance { get; set; }

    /// <summary>
    /// 变化前赠送余额
    /// </summary>
    public decimal BeforeGiftBalance { get; set; }

    /// <summary>
    /// 变化后赠送余额
    /// </summary>
    public decimal AfterGiftBalance { get; set; }

    /// <summary>
    /// 关联订单ID
    /// </summary>
    public long? OrderId { get; set; }

    /// <summary>
    /// 支付方式（1:现金 2:支付宝 3:微信 4:银行卡）
    /// </summary>
    public int? PayMethod { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 操作人ID（System 服务用户ID）
    /// 充值/消费/退款/调整场景必须填充；手动补录由 Validator 强制必填
    /// </summary>
    public long? OperatorId { get; set; }

    /// <summary>
    /// 导航属性：客户
    /// </summary>
    public Customer? Customer { get; set; }
}
