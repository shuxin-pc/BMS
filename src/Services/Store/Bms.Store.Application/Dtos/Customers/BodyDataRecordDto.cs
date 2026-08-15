namespace Bms.Store.Application.Dtos.Customers;

public class BodyDataRecordDto
{
    public long Id { get; set; }
    public long CustomerId { get; set; }
    /// <summary>
    /// 客户姓名（展示用，关联 Customer 表）
    /// </summary>
    public string? CustomerName { get; set; }
    /// <summary>
    /// 客户手机号（展示用，关联 Customer 表）
    /// </summary>
    public string? CustomerPhone { get; set; }
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
