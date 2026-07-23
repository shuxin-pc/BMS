namespace Bms.Store.Application.Dtos.Customers;

public class ServiceComparisonPhotoDto
{
    public long Id { get; set; }
    public long CustomerId { get; set; }
    public long OrderId { get; set; }
    public string? ServiceItem { get; set; }
    public DateTime PhotoDate { get; set; }
    public int PhotoType { get; set; }
    public string PhotoUrl { get; set; } = string.Empty;
    public string? Remark { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
