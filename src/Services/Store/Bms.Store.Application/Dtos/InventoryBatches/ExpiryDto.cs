namespace Bms.Store.Application.Dtos.InventoryBatches;

/// <summary>
/// 效期信息
/// </summary>
public class ExpiryDto
{
    /// <summary>
    /// 记录ID
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 商品名称
    /// </summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// 商品编码
    /// </summary>
    public string ProductCode { get; set; } = string.Empty;

    /// <summary>
    /// 批次号
    /// </summary>
    public string BatchNo { get; set; } = string.Empty;

    /// <summary>
    /// 过期日期
    /// </summary>
    public DateTime? ExpirationDate { get; set; }

    /// <summary>
    /// 采购日期
    /// </summary>
    public DateTime? PurchaseDate { get; set; }

    /// <summary>
    /// 剩余天数（负数表示已过期）。null 表示"无效期限制"批次，无剩余天数概念
    /// </summary>
    public int? RemainingDays { get; set; }

    /// <summary>
    /// 效期状态（normal:正常 expiring:即将过期 expired:已过期）
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// 门店名称
    /// </summary>
    public string? StoreName { get; set; }

    /// <summary>
    /// 批次创建时间。无效期批次排末尾按此字段升序展示，也用于有日期批次的稳定兜底排序
    /// </summary>
    public DateTime CreatedTime { get; set; }
}
