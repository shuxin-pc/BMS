namespace Bms.Store.Application.Dtos.TreatmentCards;

/// <summary>
/// 疗程卡销售记录 DTO
/// </summary>
public class TreatmentCardSaleDto
{
    public long Id { get; set; }

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
    /// 累计已消费金额
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
    public int Status { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 疗程卡包含的项目明细（含购买时锁定的折算单价）
    /// </summary>
    public List<TreatmentCardSaleItemDto> Items { get; set; } = new();

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}
