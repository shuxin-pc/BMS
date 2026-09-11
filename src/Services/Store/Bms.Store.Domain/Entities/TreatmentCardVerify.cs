namespace Bms.Store.Domain.Entities;

/// <summary>
/// 项目卡核销记录
/// </summary>
public class TreatmentCardVerify : StoreBusinessEntityBase
{
    /// <summary>
    /// 项目卡销售ID
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
    /// 操作员姓名（冗余存储，写入时取 ICurrentUser.RealName ?? UserName）
    /// 冗余原因：Store 与 System 为独立服务，且用户可能改名/离职，历史核销记录需保留操作当时的姓名快照
    /// </summary>
    public string? OperatorName { get; set; }

    /// <summary>
    /// 是否跨店核销（核销门店 ≠ 发卡门店时为 true，便于报表过滤）
    /// </summary>
    public bool IsCrossStore { get; set; }

    /// <summary>
    /// 冲正状态（0-正常，1-已冲正）
    /// 冲正时不物理删除，仅更新状态，冲正金额冲减原核销门店服务业绩
    /// </summary>
    public int ReverseStatus { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 购物车结算批次号（POS 购物车一次结算生成，用于订单/核销/开卡三单据聚合追溯）
    /// 非购物车结算（独立核销）为空
    /// </summary>
    public string? CheckoutSessionNo { get; set; }

    /// <summary>
    /// 导航属性：项目卡销售
    /// </summary>
    public TreatmentCardSale? CardSale { get; set; }

    /// <summary>
    /// 导航属性：核销项目明细（一次核销可包含多个项目）
    /// </summary>
    public List<TreatmentCardVerifyItem> Items { get; set; } = new();
}
