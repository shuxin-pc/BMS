namespace Bms.Store.Domain.Entities;

/// <summary>
/// 库存盘点记录
/// 记录每次盘点的账面数量、实际数量和差异，支持草稿->已完成/已取消的状态机闭环
/// </summary>
public class InventoryCheck : StoreBusinessEntityBase
{
    /// <summary>
    /// 商品ID
    /// </summary>
    public long ProductId { get; set; }

    /// <summary>
    /// 盘点前账面库存
    /// </summary>
    public decimal BeforeQuantity { get; set; }

    /// <summary>
    /// 实际盘点数量
    /// </summary>
    public decimal ActualQuantity { get; set; }

    /// <summary>
    /// 差异数量（实际-账面，正数为盘盈，负数为盘亏）
    /// 提交盘点时由系统自动计算
    /// </summary>
    public decimal DiffQuantity { get; set; }

    /// <summary>
    /// 盘点时间
    /// </summary>
    public DateTime CheckTime { get; set; }

    /// <summary>
    /// 操作员ID
    /// </summary>
    public long? OperatorId { get; set; }

    /// <summary>
    /// 盘点单状态（0=草稿 1=已完成 2=已取消）
    /// 草稿：创建后初始状态，可修改/提交/取消
    /// 已完成：提交后终态，库存已联动调整
    /// 已取消：草稿取消，终态，不调整库存
    /// </summary>
    public int Status { get; set; } = 0;

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 导航属性：商品
    /// </summary>
    public Product? Product { get; set; }
}
