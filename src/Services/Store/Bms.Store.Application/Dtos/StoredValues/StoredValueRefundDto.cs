namespace Bms.Store.Application.Dtos.StoredValues;

/// <summary>
/// 储值退款请求 DTO
/// 退款冲减原充值门店充值业绩，门店关店则冲减当前操作门店（规则8）
/// 退款只退实收余额(RealBalance)，赠送余额(GiftBalance)一律不退（文档 G7），退款上限为剩余实收余额
/// </summary>
public class StoredValueRefundDto
{
    /// <summary>
    /// 客户ID
    /// </summary>
    public long CustomerId { get; set; }

    /// <summary>
    /// 退款金额（必须 > 0）
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// 退款支付方式（1:现金 2:支付宝 3:微信 4:银行卡）
    /// </summary>
    public int? PayMethod { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}
