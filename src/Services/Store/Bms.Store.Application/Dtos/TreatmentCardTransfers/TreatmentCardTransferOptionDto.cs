namespace Bms.Store.Application.Dtos.TreatmentCardTransfers;

/// <summary>
/// 可转让项目卡销售选项 DTO（转卡弹窗选择项目卡用）
/// 仅返回状态有效且有剩余次数的卡销售记录
/// </summary>
public class TreatmentCardTransferOptionDto
{
    /// <summary>
    /// 项目卡销售记录ID
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 项目卡名称（join TreatmentCard 展示）
    /// </summary>
    public string? CardName { get; set; }

    /// <summary>
    /// 当前客户ID（卡归属客户，即转让的原客户）
    /// </summary>
    public long CustomerId { get; set; }

    /// <summary>
    /// 当前客户名称（join Customer 展示）
    /// </summary>
    public string? CustomerName { get; set; }

    /// <summary>
    /// 当前客户手机号（join Customer 展示）
    /// </summary>
    public string? CustomerPhone { get; set; }

    /// <summary>
    /// 剩余次数
    /// </summary>
    public int RemainingTimes { get; set; }

    /// <summary>
    /// 总次数（卡配置 TotalTimes）
    /// </summary>
    public int TotalTimes { get; set; }
}
