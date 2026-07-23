namespace Bms.Store.Application.Dtos.Customers;

/// <summary>
/// 客户美容档案输出 DTO
/// </summary>
public class CustomerBeautyProfileDto
{
    public long Id { get; set; }
    public long CustomerId { get; set; }
    public string? SkinType { get; set; }
    public string? Sensitivity { get; set; }
    public string? HairType { get; set; }
    public string? AllergyHistory { get; set; }
    public string? Remark { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
