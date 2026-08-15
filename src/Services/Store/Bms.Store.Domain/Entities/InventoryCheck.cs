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
    /// 操作员姓名（冗余存储，写入时取 ICurrentUser.RealName ?? UserName）
    /// </summary>
    public string? OperatorName { get; set; }

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
    /// 批次号（盘亏时取首批扣减批次号；盘盈时取新建盘盈批次号；无差异时为空）
    /// 提交盘点时由后端写入，用于追溯本次盘点影响的批次
    /// </summary>
    public string? BatchNo { get; set; }

    /// <summary>
    /// 单价（盘亏时取首批扣减批次的 UnitPrice；盘盈时取录入的估值单价）
    /// 用于 DiffAmount 计算，与 InventoryLog.UnitPrice 一致
    /// </summary>
    public decimal? UnitPrice { get; set; }

    /// <summary>
    /// 过期日期（盘亏时取首批扣减批次的过期日期；盘盈时取录入的过期日期）
    /// 与 InventoryLog.ExpirationDate 一致
    /// </summary>
    public DateTime? ExpirationDate { get; set; }

    /// <summary>
    /// 差异金额（提交时持久化，按真实批次单价计算）
    /// 盘亏 = -Σ(扣减数量 × 批次 UnitPrice)，负数
    /// 盘盈 = 盘盈数量 × 录入 UnitPrice，正数
    /// 无差异时为 null
    /// </summary>
    public decimal? DiffAmount { get; set; }

    /// <summary>
    /// 导航属性：商品
    /// </summary>
    public Product? Product { get; set; }
}
