namespace Bms.Store.Application.Dtos.StoredValues;

/// <summary>
/// 充值赠送金额试算结果 DTO（充值弹窗实时展示用）
/// </summary>
public class StoredValueGiftPreviewDto
{
    /// <summary>
    /// 试算的充值金额
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// 按储值规则计算出的赠送金额
    /// </summary>
    public decimal GiftAmount { get; set; }
}
