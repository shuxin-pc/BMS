namespace Bms.Store.Domain.Entities;

/// <summary>
/// 疗程卡销售记录
/// </summary>
public class TreatmentCardSale : StoreTenantEntityBase
{
    /// <summary>
    /// 购买门店ID（可空，疗程卡在租户内跨店通用）
    /// </summary>
    public long? StoreId { get; set; }

    /// <summary>
    /// 购买门店编码
    /// </summary>
    public string? StoreCode { get; set; }

    /// <summary>
    /// 疗程卡ID
    /// </summary>
    public long CardId { get; set; }

    /// <summary>
    /// 客户ID
    /// </summary>
    public long CustomerId { get; set; }

    /// <summary>
    /// 购买日期
    /// </summary>
    public DateTime PurchaseDate { get; set; }

    /// <summary>
    /// 购买金额
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// 累计已消费金额（按折算单价累计，用于"最后一次核销兜底"和退款计算）
    /// </summary>
    public decimal TotalConsumedAmount { get; set; }

    /// <summary>
    /// 剩余次数
    /// </summary>
    public int RemainingTimes { get; set; }

    /// <summary>
    /// 有效期至
    /// </summary>
    public DateTime ExpiryDate { get; set; }

    /// <summary>
    /// 状态（1:有效 2:已用完 3:已过期）
    /// </summary>
    public int Status { get; set; } = 1;

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 导航属性：疗程卡
    /// </summary>
    public TreatmentCard? Card { get; set; }

    /// <summary>
    /// 导航属性：客户
    /// </summary>
    public Customer? Customer { get; set; }

    /// <summary>
    /// 导航属性：疗程卡包含的项目明细（购买时按实际售价分摊计算折算单价并锁定）
    /// </summary>
    public List<TreatmentCardSaleItem> Items { get; set; } = new();
}
