using Bms.Store.Application.Dtos;

namespace Bms.Store.Application.Dtos.Customers;

public class CustomerPreferenceQueryDto : PagedRequestDto
{
    public long? CustomerId { get; set; }
}
