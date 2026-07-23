namespace Bms.Store.Application.Dtos.Technicians;

/// <summary>
/// 创建技师统计输入 DTO
/// </summary>
public class TechnicianStatisticCreateDto
{
    public long TechnicianId { get; set; }
    public DateTime StatDate { get; set; }
    public int ServiceCount { get; set; }
    public int ServiceMinutes { get; set; }
    public decimal TotalTechnicianFee { get; set; }
    public int ReturnCustomerCount { get; set; }
}
