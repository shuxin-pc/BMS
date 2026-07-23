namespace Bms.Store.Domain.Entities;

/// <summary>
/// 库存预警
/// </summary>
public class InventoryAlert : StoreBusinessEntityBase
{
    /// <summary>
    /// 商品ID
    /// </summary>
    public long ProductId { get; set; }

    /// <summary>
    /// 预警类型（1:库存不足 2:效期预警 3:积压预警）
    /// </summary>
    public int AlertType { get; set; }

    /// <summary>
    /// 当前库存
    /// </summary>
    public decimal CurrentQuantity { get; set; }

    /// <summary>
    /// 预警值
    /// </summary>
    public decimal AlertValue { get; set; }

    /// <summary>
    /// 过期日期
    /// </summary>
    public DateTime? ExpirationDate { get; set; }

    /// <summary>
    /// 是否已处理
    /// </summary>
    public bool IsProcessed { get; set; }

    /// <summary>
    /// 处理时间
    /// </summary>
    public DateTime? ProcessedTime { get; set; }

    /// <summary>
    /// 处理备注
    /// </summary>
    public string? ProcessedRemark { get; set; }

    /// <summary>
    /// 导航属性：商品
    /// </summary>
    public Product? Product { get; set; }
}
