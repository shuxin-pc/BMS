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
    /// 赠送金额由后端按储值规则计算，不接受前端传入，避免绕过规则
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// 支付方式（1:现金 2:支付宝 3:微信 4:银行卡）
    /// </summary>
    public int? PayMethod { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}
