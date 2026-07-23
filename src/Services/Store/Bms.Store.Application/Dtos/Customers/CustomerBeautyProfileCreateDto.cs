namespace Bms.Store.Application.Dtos.Customers;

/// <summary>
/// 创建客户美容档案输入 DTO
/// </summary>
public class CustomerBeautyProfileCreateDto
{
    public long CustomerId { get; set; }
    public string? SkinType { get; set; }
    public string? Sensitivity { get; set; }
    public string? HairType { get; set; }
    public string? AllergyHistory { get; set; }
    public string? Remark { get; set; }
}
