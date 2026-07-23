namespace Bms.Store.Domain.Entities;

/// <summary>
/// 价格变更记录
/// 记录商品价格变更历史，支持价格追溯
/// </summary>
public class PriceChangeLog : StoreBusinessEntityBase
{
    /// <summary>
    /// 商品ID
    /// </summary>
    public long ProductId { get; set; }

    /// <summary>
    /// 原价格
    /// </summary>
    public decimal OldPrice { get; set; }

    /// <summary>
    /// 新价格
    /// </summary>
    public decimal NewPrice { get; set; }

    /// <summary>
    /// 变更时间
    /// </summary>
    public DateTime ChangeTime { get; set; }

    /// <summary>
    /// 操作员ID
    /// </summary>
    public long? OperatorId { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 导航属性：商品
    /// </summary>
    public Product? Product { get; set; }
}
