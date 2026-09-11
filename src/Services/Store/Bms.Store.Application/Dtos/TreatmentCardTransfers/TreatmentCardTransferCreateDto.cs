namespace Bms.Store.Application.Dtos.TreatmentCardTransfers;

/// <summary>
/// 创建项目卡转让请求 DTO
/// </summary>
public class TreatmentCardTransferCreateDto
{
    /// <summary>
    /// 操作门店ID（可空，项目卡跨店通用）
    /// </summary>
    public long? StoreId { get; set; }

    /// <summary>
    /// 操作门店编码
    /// </summary>
    public string? StoreCode { get; set; }

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
    /// 状态（1:已转让）
    /// </summary>
    public int Status { get; set; } = 1;

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}
