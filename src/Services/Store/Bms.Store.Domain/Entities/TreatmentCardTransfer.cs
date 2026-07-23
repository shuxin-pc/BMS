namespace Bms.Store.Domain.Entities;

/// <summary>
/// 疗程卡转让记录
/// 记录疗程卡在租户内的转让操作（MVP 支持租户内转让）
/// </summary>
public class TreatmentCardTransfer : StoreTenantEntityBase
{
    /// <summary>
    /// 操作门店ID（可空，疗程卡跨店通用）
    /// </summary>
    public long? StoreId { get; set; }

    /// <summary>
    /// 操作门店编码
    /// </summary>
    public string? StoreCode { get; set; }

    /// <summary>
    /// 疗程卡销售记录ID
    /// </summary>
    public long CardSaleId { get; set; }

    /// <summary>
    /// 原客户ID
    /// </summary>
    public long FromCustomerId { get; set; }

    /// <summary>
    /// 新客户ID
    /// </summary>
    public long ToCustomerId { get; set; }

    /// <summary>
    /// 转让日期
    /// </summary>
    public DateTime TransferDate { get; set; }

    /// <summary>
    /// 转让手续费
    /// </summary>
    public decimal TransferFee { get; set; }

    /// <summary>
    /// 操作员ID
    /// </summary>
    public long? OperatorId { get; set; }

    /// <summary>
    /// 状态（1:已转让）
    /// </summary>
    public int Status { get; set; } = 1;

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 导航属性：疗程卡销售记录
    /// </summary>
    public TreatmentCardSale? CardSale { get; set; }

    /// <summary>
    /// 导航属性：原客户
    /// </summary>
    public Customer? FromCustomer { get; set; }

    /// <summary>
    /// 导航属性：新客户
    /// </summary>
    public Customer? ToCustomer { get; set; }
}
