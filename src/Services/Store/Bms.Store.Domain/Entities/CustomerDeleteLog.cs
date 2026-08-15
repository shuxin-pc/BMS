namespace Bms.Store.Domain.Entities;

/// <summary>
/// 客户档案永久删除审计日志
/// 永久保留，不参与软删除，用于合规审计追溯
/// 依据：《个人信息保护法》第 47 条要求删除个人信息但保留审计记录
/// </summary>
public class CustomerDeleteLog : StoreBusinessEntityBase
{
    /// <summary>
    /// 原客户ID（已物理删除）
    /// </summary>
    public long OriginalCustomerId { get; set; }

    /// <summary>
    /// 原客户姓名（脱敏前快照）
    /// </summary>
    public string OriginalName { get; set; } = string.Empty;

    /// <summary>
    /// 原客户手机号（脱敏前快照）
    /// </summary>
    public string OriginalPhone { get; set; } = string.Empty;

    /// <summary>
    /// 操作人ID（System 服务用户ID）
    /// </summary>
    public long? OperatorId { get; set; }

    /// <summary>
    /// 删除时间
    /// </summary>
    public DateTime DeleteTime { get; set; }

    /// <summary>
    /// 删除原因
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}
