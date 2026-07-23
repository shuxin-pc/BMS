namespace Bms.Store.Application.Dtos.Customers;

public class BodyDataRecordCreateDto
{
    public long CustomerId { get; set; }
    public DateTime RecordDate { get; set; }
    public decimal? Weight { get; set; }
    public decimal? BodyFat { get; set; }
    public decimal? Bust { get; set; }
    public decimal? Waist { get; set; }
    public decimal? Hip { get; set; }
    public string? Remark { get; set; }
}
