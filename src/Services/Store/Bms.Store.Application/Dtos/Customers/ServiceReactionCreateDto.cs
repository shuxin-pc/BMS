namespace Bms.Store.Application.Dtos.Customers;

public class ServiceReactionCreateDto
{
    public long CustomerId { get; set; }
    public long? OrderId { get; set; }
    public string? ServiceItem { get; set; }
    public DateTime ReactionDate { get; set; }
    public string? Reaction { get; set; }
    public int? Severity { get; set; }
    public string? Remark { get; set; }
}
