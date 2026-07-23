namespace Bms.Store.Application.Dtos.Technicians;

/// <summary>
/// 技师统计输出 DTO
/// </summary>
public class TechnicianStatisticDto
{
    public long Id { get; set; }
    public long TechnicianId { get; set; }
    public DateTime StatDate { get; set; }
    public int ServiceCount { get; set; }
    public int ServiceMinutes { get; set; }
    public decimal TotalTechnicianFee { get; set; }
    public int ReturnCustomerCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
