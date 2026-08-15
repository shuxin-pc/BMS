namespace Bms.Store.Application.Dtos.StoredValues;

/// <summary>
/// 储值账户 DTO
/// </summary>
public class StoredValueAccountDto
{
    public long Id { get; set; }

    /// <summary>
    /// 客户ID
    /// </summary>
    public long CustomerId { get; set; }

    /// <summary>
    /// 客户名称（关联客户档案，客户已删除时为空）
    /// </summary>
    public string? CustomerName { get; set; }

    /// <summary>
    /// 客户手机号（关联客户档案，客户已删除时为空）
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// 当前余额
    /// </summary>
    public decimal Balance { get; set; }

    /// <summary>
    /// 实收余额
    /// </summary>
    public decimal RealBalance { get; set; }

    /// <summary>
    /// 赠送余额
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

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
