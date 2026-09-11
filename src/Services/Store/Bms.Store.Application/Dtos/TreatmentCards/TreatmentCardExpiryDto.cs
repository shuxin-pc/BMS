namespace Bms.Store.Application.Dtos.TreatmentCards;

/// <summary>
/// 项目卡到期提醒信息
/// </summary>
public class TreatmentCardExpiryDto
{
    /// <summary>
    /// 记录ID（同销售记录ID）
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 客户名称
    /// </summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>
    /// 手机号
    /// </summary>
    public string Phone { get; set; } = string.Empty;

    /// <summary>
    /// 卡名称
    /// </summary>
    public string CardName { get; set; } = string.Empty;

    /// <summary>
    /// 销售记录ID
    /// </summary>
    public long SaleId { get; set; }

    /// <summary>
    /// 购买日期
    /// </summary>
    public DateTime PurchaseDate { get; set; }

    /// <summary>
    /// 到期日期
    /// </summary>
    public DateTime ExpiryDate { get; set; }

    /// <summary>
    /// 剩余天数（负数表示已过期）
    /// </summary>
    public int RemainingDays { get; set; }

    /// <summary>
    /// 剩余次数
    /// </summary>
    public int RemainingTimes { get; set; }

    /// <summary>
    /// 预警级别（1:即将到期 2:已到期）
    /// </summary>
    public int AlertLevel { get; set; }

    /// <summary>
    /// 状态（1:有效 2:已用完 3:已过期）
    /// </summary>
    public int Status { get; set; }
}
