namespace Bms.Store.Application.Dtos.Customers;

/// <summary>
/// 消费感谢输出 DTO
/// Id 字段填充订单ID，供前端标记感谢时回传
/// </summary>
public class ConsumeThankRecordDto
{
    /// <summary>
    /// 订单ID（作为列表项业务ID，标记感谢时回传）
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 客户ID
    /// </summary>
    public long CustomerId { get; set; }

    /// <summary>
    /// 客户姓名
    /// </summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>
    /// 手机号
    /// </summary>
    public string Phone { get; set; } = string.Empty;

    /// <summary>
    /// 近 7 天累计消费金额
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// 近 7 天消费订单笔数
    /// </summary>
    public int OrderCount { get; set; }

    /// <summary>
    /// 最近消费时间
    /// </summary>
    public DateTime LastConsumeTime { get; set; }

    /// <summary>
    /// 感谢状态：1=待感谢 2=已感谢
    /// </summary>
    public int ThankStatus { get; set; }

    /// <summary>
    /// 感谢方式：1=短信 2=微信 3=电话
    /// </summary>
    public int? ThankMethod { get; set; }

    /// <summary>
    /// 感谢时间
    /// </summary>
    public DateTime? ThankTime { get; set; }

    /// <summary>
    /// 操作人姓名（已感谢时显示）
    /// </summary>
    public string? OperatorName { get; set; }
}
