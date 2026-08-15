namespace Bms.Store.Domain.Entities;

/// <summary>
/// 客户等级
/// 等级数量与等级值由门店自由管理，同租户内 Level 值唯一
/// </summary>
public class CustomerLevel : StoreEntity
{
    /// <summary>
    /// 等级名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 等级编码
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 等级值（同租户内唯一，由门店自定义，表示等级高低）
    /// </summary>
    public int Level { get; set; }

    /// <summary>
    /// 折扣率
    /// </summary>
    public decimal DiscountRate { get; set; } = 1.0m;

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}
