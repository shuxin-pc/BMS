namespace Bms.Store.Application.Dtos.Customers;

public class ServiceReactionDto
{
    public long Id { get; set; }
    public long CustomerId { get; set; }
    public long? OrderId { get; set; }
    public string? ServiceItem { get; set; }
    public DateTime ReactionDate { get; set; }
    public string? Reaction { get; set; }
    public int? Severity { get; set; }
    public string? Remark { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
