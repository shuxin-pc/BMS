using Bms.Store.Application.Dtos;

namespace Bms.Store.Application.Dtos.Customers;

public class ServiceComparisonPhotoQueryDto : PagedRequestDto
{
    public long? CustomerId { get; set; }
}
