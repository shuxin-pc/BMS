namespace Bms.Store.Application.Dtos.TreatmentCards;

/// <summary>
/// 项目卡销售项目明细 DTO
/// </summary>
public class TreatmentCardSaleItemDto
{
    public long Id { get; set; }

    /// <summary>
    /// 项目卡销售记录ID
    /// </summary>
    public long SaleId { get; set; }

    /// <summary>
    /// 商品ID
    /// </summary>
    public long ProductId { get; set; }

    /// <summary>
    /// 该项目在项目卡中的次数
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// 项目原价
    /// </summary>
    public decimal OriginalPrice { get; set; }

    /// <summary>
    /// 折算单价（购买时锁定）
    /// </summary>
    public decimal AllocatedUnitPrice { get; set; }

    /// <summary>
    /// 分摊总价值
    /// </summary>
    public decimal AllocatedTotalPrice { get; set; }

    /// <summary>
    /// 该项目剩余可核销次数（= Quantity - 该卡该项目已核销次数，列表/详情接口聚合核销明细补充，排除已冲正记录）
    /// 核销弹窗用于限制单一项目核销次数不超过其在项目卡中的剩余次数
    /// </summary>
    public int RemainingQuantity { get; set; }

    /// <summary>
    /// 该项目已核销累计金额（列表/详情接口聚合核销明细金额补充，排除已冲正记录）
    /// 核销弹窗用于该项目最后一次核销时的兜底金额计算：分摊总价值 - 已核销累计金额（B2 小数处理规则）
    /// </summary>
    public decimal ConsumedAmount { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}
