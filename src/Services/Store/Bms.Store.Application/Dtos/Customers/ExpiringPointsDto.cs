namespace Bms.Store.Application.Dtos.Customers;

/// <summary>
/// 即将过期积分明细 DTO
/// 用于客户详情页展示"即将过期积分"提醒徽章
/// </summary>
public class ExpiringPointsDto
{
    /// <summary>
    /// 积分流水ID
    /// </summary>
    public long LogId { get; set; }

    /// <summary>
    /// 积分数量
    /// </summary>
    public int Points { get; set; }

    /// <summary>
    /// 过期日期
    /// </summary>
    public DateTime ExpireDate { get; set; }

    /// <summary>
    /// 剩余天数（负数表示已过期）
    /// </summary>
    public int DaysRemaining { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}
