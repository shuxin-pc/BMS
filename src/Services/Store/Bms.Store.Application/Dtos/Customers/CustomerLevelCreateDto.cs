namespace Bms.Store.Application.Dtos.Customers;

/// <summary>
/// 创建客户等级输入 DTO
/// </summary>
public class CustomerLevelCreateDto
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 等级值（同租户内唯一，由门店自定义）
    /// </summary>
    public int Level { get; set; }

    public decimal DiscountRate { get; set; } = 1.0m;
    public string? Remark { get; set; }
}
