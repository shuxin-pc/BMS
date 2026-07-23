namespace Bms.Store.Application.Dtos.Points;

/// <summary>
/// 积分兑换记录 DTO
/// </summary>
public class PointsExchangeDto
{
    public long Id { get; set; }

    /// <summary>
    /// 客户ID
    /// </summary>
    public long CustomerId { get; set; }

    /// <summary>
    /// 兑换类型（1:商品 2:优惠券）
    /// </summary>
    public int ExchangeType { get; set; }

    /// <summary>
    /// 兑换目标ID（商品ID或优惠券ID）
    /// </summary>
    public long? TargetId { get; set; }

    /// <summary>
    /// 兑换目标名称
    /// </summary>
    public string TargetName { get; set; } = string.Empty;

    /// <summary>
    /// 消耗积分
    /// </summary>
    public int PointsCost { get; set; }

    /// <summary>
    /// 兑换数量
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// 兑换时间
    /// </summary>
    public DateTime ExchangeTime { get; set; }

    /// <summary>
    /// 操作员ID
    /// </summary>
    public long? OperatorId { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
