namespace Bms.Store.Application.Dtos.Customers;

public class ServiceComparisonPhotoCreateDto
{
    public long CustomerId { get; set; }
    public long OrderId { get; set; }
    public string? ServiceItem { get; set; }
    public DateTime PhotoDate { get; set; }
    public int PhotoType { get; set; }
    public string PhotoUrl { get; set; } = string.Empty;
    public string? Remark { get; set; }
}
