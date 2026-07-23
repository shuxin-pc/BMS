namespace Bms.Store.Application.Dtos.Customers;

/// <summary>
/// 客户等级输出 DTO
/// </summary>
public class CustomerLevelDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 等级值（1:普通会员 2:会员）
    /// </summary>
    public int Level { get; set; }

    public decimal DiscountRate { get; set; }
    public int Sort { get; set; }
    public string? Remark { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
