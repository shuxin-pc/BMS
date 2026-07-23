namespace Bms.Store.Application.Dtos.PointsRules;

/// <summary>
/// 积分规则DTO
/// </summary>
public class PointsRuleDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal PointsRate { get; set; }
    public decimal DeductRate { get; set; }
    public decimal MaxDeductAmount { get; set; }
    public int? PointsValidityDays { get; set; }
    public bool BirthdayDouble { get; set; }
    public decimal? MinPointsThreshold { get; set; }
    public int Status { get; set; }
    public string? Remark { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
