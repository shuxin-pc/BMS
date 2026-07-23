namespace Bms.Store.Application.Dtos.TreatmentCards;

/// <summary>
/// 创建疗程卡销售记录请求 DTO
/// </summary>
public class TreatmentCardSaleCreateDto
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
    /// 状态（1:有效 2:已用完 3:已过期）
    /// </summary>
    public int Status { get; set; } = 1;

    /// <summary>
    /// 疗程卡包含的项目列表（前端传入商品、次数、原价；折算单价由后端按实际购买金额分摊计算并锁定）
    /// </summary>
    public List<TreatmentCardSaleItemCreateDto> Items { get; set; } = new();

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}
