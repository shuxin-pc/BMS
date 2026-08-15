namespace Bms.Store.Application.Dtos.TreatmentCards;

/// <summary>
/// 疗程卡退卡请求 DTO
/// 退卡时全额冲减发卡门店销售业绩，已发生的核销业绩不冲回（规则6）
/// 退卡金额 = 售价 - 已核销金额（未消费部分退还客户）
/// </summary>
public class TreatmentCardSaleRefundDto
{
    /// <summary>
    /// 疗程卡销售记录ID
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 退卡原因（必填，便于审计追溯）
    /// </summary>
    public string Remark { get; set; } = string.Empty;
}

/// <summary>
/// 退卡结果 DTO
/// </summary>
public class TreatmentCardSaleRefundResultDto
{
    /// <summary>
    /// 销售记录ID
    /// </summary>
    public long SaleId { get; set; }

    /// <summary>
    /// 原售价
    /// </summary>
    public decimal OriginalAmount { get; set; }

    /// <summary>
    /// 已消费金额（不冲回，服务已实际发生）
    /// </summary>
    public decimal ConsumedAmount { get; set; }

    /// <summary>
    /// 应退金额（售价 - 已消费金额）
    /// </summary>
    public decimal RefundAmount { get; set; }

    /// <summary>
    /// 退卡后状态（3:已退卡）
    /// </summary>
    public int Status { get; set; }
}
