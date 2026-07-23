namespace Bms.Store.Domain.Entities;

/// <summary>
/// 疗程卡核销项目明细
/// 一次核销可包含多个项目（一次到店做多种护理），每个项目独立计算金额
/// </summary>
public class TreatmentCardVerifyItem : StoreTenantEntityBase
{
    /// <summary>
    /// 核销主单ID
    /// </summary>
    public long VerifyId { get; set; }

    /// <summary>
    /// 核销的商品ID（服务项目，对应 TreatmentCardSaleItem.ProductId）
    /// </summary>
    public long ProductId { get; set; }

    /// <summary>
    /// 本项核销次数（默认 1，至少 1）
    /// </summary>
    public int VerifyTimes { get; set; } = 1;

    /// <summary>
    /// 折算单价（核销时从 TreatmentCardSaleItem.AllocatedUnitPrice 复制并锁定）
    /// </summary>
    public decimal AllocatedUnitPrice { get; set; }

    /// <summary>
    /// 本项核销金额 = AllocatedUnitPrice × VerifyTimes
    /// 最后一次核销时此项会应用兜底逻辑（剩余金额全部分摊）
    /// </summary>
    public decimal SubAmount { get; set; }

    /// <summary>
    /// 导航属性：核销主单
    /// </summary>
    public TreatmentCardVerify? Verify { get; set; }

    /// <summary>
    /// 导航属性：商品
    /// </summary>
    public Product? Product { get; set; }
}
