namespace Bms.Store.Application.Dtos.PointsRules;

/// <summary>
/// 创建积分规则DTO
/// </summary>
public class PointsRuleCreateDto
{
    public decimal PointsRate { get; set; }
    public decimal DeductRate { get; set; }
    public decimal MaxDeductAmount { get; set; }
    public int? PointsValidityDays { get; set; }
    public bool BirthdayDouble { get; set; }
    public decimal? MinAmountThreshold { get; set; }
    public int Status { get; set; } = 1;
    public string? Remark { get; set; }
}
