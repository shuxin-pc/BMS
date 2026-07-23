namespace Bms.Store.Application.Dtos.Customers;

/// <summary>
/// 创建客户等级输入 DTO
/// </summary>
public class CustomerLevelCreateDto
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 等级值（1:普通会员 2:会员），创建后不可修改
    /// </summary>
    public int Level { get; set; }

    public decimal DiscountRate { get; set; } = 1.0m;
    public int Sort { get; set; }
    public string? Remark { get; set; }
}
