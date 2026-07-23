namespace Bms.Store.Application.Dtos.Customers;

public class CustomerPreferenceCreateDto
{
    public long CustomerId { get; set; }
    public string? TechniquePressure { get; set; }
    public string? Temperature { get; set; }
    public string? MusicPreference { get; set; }
    public long? PreferredTechnicianId { get; set; }
    public string? Remark { get; set; }
}
