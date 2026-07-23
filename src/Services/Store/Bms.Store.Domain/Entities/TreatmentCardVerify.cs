namespace Bms.Store.Domain.Entities;

/// <summary>
/// 疗程卡核销记录
/// </summary>
public class TreatmentCardVerify : StoreTenantEntityBase
{
    /// <summary>
    /// 核销门店ID（记录在哪家门店核销，疗程卡跨店通用）
    /// </summary>
    public long? StoreId { get; set; }

    /// <summary>
    /// 核销门店编码
    /// </summary>
    public string? StoreCode { get; set; }

    /// <summary>
    /// 疗程卡销售ID
    /// </summary>
    public long CardSaleId { get; set; }

    /// <summary>
    /// 本次核销总金额（冗余字段，等于 Items.Sum(SubAmount)，便于列表查询避免聚合）
    /// </summary>
    public decimal VerifyAmount { get; set; }

    /// <summary>
    /// 本次核销总次数（冗余字段，等于 Items.Sum(VerifyTimes)）
    /// </summary>
    public int VerifyTimes { get; set; } = 1;

    /// <summary>
    /// 关联订单ID（核销时创建一笔订单，此处关联）
    /// </summary>
    public long? OrderId { get; set; }

    /// <summary>
    /// 核销时间
    /// </summary>
    public DateTime VerifyTime { get; set; }

    /// <summary>
    /// 操作员ID
    /// </summary>
    public long? OperatorId { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 导航属性：疗程卡销售
    /// </summary>
    public TreatmentCardSale? CardSale { get; set; }

    /// <summary>
    /// 导航属性：核销项目明细（一次核销可包含多个项目）
    /// </summary>
    public List<TreatmentCardVerifyItem> Items { get; set; } = new();
}
