namespace Bms.Store.Application.Dtos.InventoryBatches;

/// <summary>
/// 商品可用效期选项（用于 POS 效期选择界面）
/// 有日期批次按过期日期升序在前，无效期批次排末尾按 CreatedTime 升序
/// </summary>
public class ProductExpiryOptionDto
{
    /// <summary>
    /// 过期日期。null 表示"无效期限制"批次（未填到期日期的入库批次）
    /// </summary>
    public DateTime? ExpirationDate { get; set; }

    /// <summary>
    /// 该效期下所有在库批次的总库存数量
    /// </summary>
    public decimal TotalQuantity { get; set; }

    /// <summary>
    /// 最早采购日期（用于展示，同效期多批次时取最早）
    /// </summary>
    public DateTime? EarliestPurchaseDate { get; set; }

    /// <summary>
    /// 剩余天数（负数表示已过期）。null 表示"无效期限制"批次，无剩余天数概念
    /// </summary>
    public int? RemainingDays { get; set; }

    /// <summary>
    /// 是否推荐（近效期优先，列表中第一项为 true）
    /// </summary>
    public bool IsRecommended { get; set; }

    /// <summary>
    /// 是否为"无效期限制"批次（未填到期日期）。true 时前端展示"无效期限制"字样并排末尾
    /// </summary>
    public bool IsNoExpiry { get; set; }

    /// <summary>
    /// 批次创建时间。无效期批次之间按此字段升序排序
    /// </summary>
    public DateTime CreatedTime { get; set; }
}
