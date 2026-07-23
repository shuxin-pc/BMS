namespace Bms.Store.Application.Dtos.TreatmentCards;

/// <summary>
/// 疗程卡核销记录 DTO
/// </summary>
public class TreatmentCardVerifyDto
{
    public long Id { get; set; }

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
    /// 本次核销总金额（冗余字段，等于 Items.Sum(SubAmount)）
    /// </summary>
    public decimal VerifyAmount { get; set; }

    /// <summary>
    /// 关联订单ID
    /// </summary>
    public long? OrderId { get; set; }

    /// <summary>
    /// 本次核销总次数（冗余字段，等于 Items.Sum(VerifyTimes)）
    /// </summary>
    public int VerifyTimes { get; set; }

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
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// 核销项目明细列表（一次核销可包含多个项目）
    /// </summary>
    public List<TreatmentCardVerifyItemDto> Items { get; set; } = new();
}

/// <summary>
/// 疗程卡核销项目明细 DTO（输出）
/// 对齐实体 TreatmentCardVerifyItem
/// </summary>
public class TreatmentCardVerifyItemDto
{
    /// <summary>
    /// 明细ID
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 核销主单ID
    /// </summary>
    public long VerifyId { get; set; }

    /// <summary>
    /// 核销的项目ID（服务项目）
    /// </summary>
    public long ProductId { get; set; }

    /// <summary>
    /// 本项核销次数
    /// </summary>
    public int VerifyTimes { get; set; }

    /// <summary>
    /// 折算单价（核销时锁定）
    /// </summary>
    public decimal AllocatedUnitPrice { get; set; }

    /// <summary>
    /// 本项核销金额 = AllocatedUnitPrice × VerifyTimes（最后一项含兜底）
    /// </summary>
    public decimal SubAmount { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
