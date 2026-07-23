namespace Bms.Store.Application.Dtos.StoredValues;

/// <summary>
/// 储值充值输入 DTO（操作人员手动录入金额，不涉及外部支付接口）
/// </summary>
public class StoredValueRechargeDto
{
    /// <summary>
    /// 客户ID
    /// </summary>
    public long CustomerId { get; set; }

    /// <summary>
    /// 充值金额（客户实际支付的金额，计入实收余额）
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// 储值规则ID（可选，选择规则时按规则自动计算赠送金额）
    /// </summary>
    public long? StoredValueRuleId { get; set; }

    /// <summary>
    /// 手动赠送金额（优先于规则，未选规则时由操作人员手动输入）
    /// </summary>
    public decimal? GiftAmount { get; set; }

    /// <summary>
    /// 支付方式（1:现金 2:支付宝 3:微信 4:银行卡）
    /// </summary>
    public int? PayMethod { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}
