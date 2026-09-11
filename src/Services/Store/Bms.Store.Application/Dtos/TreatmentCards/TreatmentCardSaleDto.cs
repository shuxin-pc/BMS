namespace Bms.Store.Application.Dtos.TreatmentCards;

/// <summary>
/// 项目卡销售记录 DTO
/// </summary>
public class TreatmentCardSaleDto
{
    public long Id { get; set; }

    /// <summary>
    /// 购买门店ID（可空，项目卡在租户内跨店通用）
    /// </summary>
    public long? StoreId { get; set; }

    /// <summary>
    /// 购买门店编码
    /// </summary>
    public string? StoreCode { get; set; }

    /// <summary>
    /// 项目卡ID
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
    /// 项目卡有效期（天），0 表示不限到期时间（列表/详情接口 join TreatmentCard 补充）
    /// </summary>
    public int ValidityDays { get; set; }

    /// <summary>
    /// 状态（1:有效 2:已用完 3:已过期）
    /// </summary>
    public int Status { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 销售单号（后端自动生成，格式：TC{yyyyMMdd}{序号}；历史数据为空）
    /// </summary>
    public string? SaleNo { get; set; }

    /// <summary>
    /// 支付方式（1:现金 2:支付宝 3:微信 4:银行卡 5:储值卡 6:积分抵扣 7:组合支付）
    /// 与订单 Order.PayMethod 枚举口径一致（POS 统一支付方式）
    /// </summary>
    public int? PayMethod { get; set; }

    /// <summary>
    /// 组合支付-类别1金额（现金/支付宝/微信/银行卡，PayMethod=7 时使用）
    /// </summary>
    public decimal? CashAmount { get; set; }

    /// <summary>
    /// 组合支付-类别1具体方式（1:现金 2:支付宝 3:微信 4:银行卡）
    /// </summary>
    public int? CashPayMethod { get; set; }

    /// <summary>
    /// 组合支付-类别2储值扣款金额（PayMethod=7 时使用）
    /// </summary>
    public decimal? StoredValueAmount { get; set; }

    /// <summary>
    /// 组合支付-类别3积分抵扣金额（PayMethod=7 时使用）
    /// </summary>
    public decimal? PointsAmount { get; set; }

    /// <summary>
    /// 客户名称（列表查询 join Customer 补充，详情/创建返回可能为空）
    /// </summary>
    public string? CustomerName { get; set; }

    /// <summary>
    /// 客户手机号（列表查询 join Customer 补充）
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// 项目卡名称（列表查询 join TreatmentCard 补充）
    /// </summary>
    public string? CardName { get; set; }

    /// <summary>
    /// 购买总次数（= Items.Sum(Quantity)，列表查询补充）
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 项目卡包含的项目明细（含购买时锁定的折算单价）
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
