using Bms.Store.Application.Dtos;

namespace Bms.Store.Application.Dtos.Customers;

public class ServiceReactionQueryDto : PagedRequestDto
{
    public long? CustomerId { get; set; }
}
