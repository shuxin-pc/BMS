namespace Bms.Store.Application.Dtos.Customers;

public class BodyDataRecordDto
{
    public long Id { get; set; }
    public long CustomerId { get; set; }
    public DateTime RecordDate { get; set; }
    public decimal? Weight { get; set; }
    public decimal? BodyFat { get; set; }
    public decimal? Bust { get; set; }
    public decimal? Waist { get; set; }
    public decimal? Hip { get; set; }
    public string? Remark { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
