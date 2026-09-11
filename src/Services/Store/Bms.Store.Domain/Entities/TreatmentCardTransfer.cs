namespace Bms.Store.Domain.Entities;

/// <summary>
/// 项目卡转让记录
/// 记录项目卡在租户内的转让操作（MVP 支持租户内转让）
/// </summary>
public class TreatmentCardTransfer : StoreBusinessEntityBase
{
    /// <summary>
    /// 项目卡销售记录ID
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
    /// 操作员姓名（冗余存储，写入时取 ICurrentUser.RealName ?? UserName）
    /// 冗余原因：Store 与 System 为独立服务，且用户可能改名/离职，历史转让记录需保留操作当时的姓名快照
    /// </summary>
    public string? OperatorName { get; set; }

    /// <summary>
    /// 状态（1:已转让）
    /// </summary>
    public int Status { get; set; } = 1;

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 导航属性：项目卡销售记录
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
