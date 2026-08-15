namespace Bms.Store.Application.Dtos.StoredValues;

/// <summary>
/// 储值现金流统计 DTO（G7.3）
/// 统计周期内新增储值、储值消费、储值退款、沉淀资金
/// </summary>
public class StoredValueCashFlowDto
{
    /// <summary>
    /// 查询起始日期
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// 查询结束日期
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// 新增储值金额（实收，Type=1 充值的 RealAmount 求和）
    /// </summary>
    public decimal TotalRecharge { get; set; }

    /// <summary>
    /// 新增赠送金额（Type=1 充值的 GiftAmount 求和）
    /// </summary>
    public decimal TotalGift { get; set; }

    /// <summary>
    /// 储值消费金额（Type=2 消费的 |Amount| 求和）
    /// </summary>
    public decimal TotalConsume { get; set; }

    /// <summary>
    /// 储值退款金额（Type=3 退款的 |Amount| 求和）
    /// </summary>
    public decimal TotalRefund { get; set; }

    /// <summary>
    /// 沉淀资金（结束日期当天的储值余额，按流水累加还原时点值）
    /// </summary>
    public decimal TotalBalance { get; set; }

    /// <summary>
    /// 沉淀资金中的实收余额
    /// </summary>
    public decimal TotalRealBalance { get; set; }

    /// <summary>
    /// 沉淀资金中的赠送余额
    /// </summary>
    public decimal TotalGiftBalance { get; set; }
}
