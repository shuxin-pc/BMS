namespace Bms.Store.Domain.Entities;

/// <summary>
/// 积分规则配置
/// 可配置积分比例、抵扣比例、单笔最高抵扣上限、积分有效期
/// </summary>
public class PointsRule : StoreEntity
{
    /// <summary>
    /// 积分比例（每消费1元获得积分数）
    /// </summary>
    public decimal PointsRate { get; set; }

    /// <summary>
    /// 抵扣比例（每积分可抵扣金额）
    /// </summary>
    public decimal DeductRate { get; set; }

    /// <summary>
    /// 单笔最高抵扣金额
    /// </summary>
    public decimal MaxDeductAmount { get; set; }

    /// <summary>
    /// 积分有效期天数（null=永久）
    /// </summary>
    public int? PointsValidityDays { get; set; }

    /// <summary>
    /// 生日双倍积分（true=客户生日当天消费双倍积分）
    /// </summary>
    public bool BirthdayDouble { get; set; }

    /// <summary>
    /// 单笔最低消费金额门槛（消费金额低于此值不发积分，null=无门槛）
    /// </summary>
    public decimal? MinAmountThreshold { get; set; }

    /// <summary>
    /// 状态（0:禁用 1:启用）
    /// </summary>
    public int Status { get; set; } = 1;

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}
