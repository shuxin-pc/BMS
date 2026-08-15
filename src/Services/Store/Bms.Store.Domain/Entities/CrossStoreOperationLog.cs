namespace Bms.Store.Domain.Entities;

/// <summary>
/// 跨店权益操作审计日志
/// 记录跨店核销、跨店消费、跨店充值、疗程卡转让等高风险操作的审计信息
/// 用于安全审计、风控分析、对账核查（文档 6.1 节）
/// 审计日志为 append-only，不提供修改/删除接口
/// </summary>
public class CrossStoreOperationLog : StoreBusinessEntityBase
{
    /// <summary>
    /// 操作类型：
    /// CrossStoreVerify-跨店核销, CrossStoreConsume-跨店消费,
    /// CrossStoreRecharge-跨店充值, TreatmentCardTransfer-疗程卡转让
    /// </summary>
    public string OperationType { get; set; } = string.Empty;

    /// <summary>
    /// 操作员ID（System 服务用户ID）
    /// </summary>
    public long OperatorId { get; set; }

    /// <summary>
    /// 操作员姓名（冗余字段，便于审计查询无需 join 用户表）
    /// </summary>
    public string? OperatorName { get; set; }

    /// <summary>
    /// 请求IP（用于安全审计追踪操作来源）
    /// </summary>
    public string? RequestIp { get; set; }

    /// <summary>
    /// 用户代理（设备指纹，识别操作设备）
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// 操作时间
    /// </summary>
    public DateTime OperationTime { get; set; }

    /// <summary>
    /// 客户ID（操作涉及的主体客户）
    /// </summary>
    public long CustomerId { get; set; }

    /// <summary>
    /// 客户姓名（冗余字段）
    /// </summary>
    public string? CustomerName { get; set; }

    /// <summary>
    /// 客户手机号后4位（身份核验记录，规则9 跨店核销客户身份验证）
    /// </summary>
    public string? CustomerPhoneTail { get; set; }

    /// <summary>
    /// 客户归属门店ID（储值账户开户门店 / 疗程卡发卡门店）
    /// 与 OperationStoreId 对比判断是否跨店
    /// </summary>
    public long? HomeStoreId { get; set; }

    /// <summary>
    /// 客户归属门店名称（冗余字段）
    /// </summary>
    public string? HomeStoreName { get; set; }

    /// <summary>
    /// 是否跨店操作（OperationStoreId ≠ HomeStoreId）
    /// </summary>
    public bool IsCrossStore { get; set; }

    /// <summary>
    /// 关联实体ID（CardSaleId/AccountId/VerifyId/TransferId 等）
    /// </summary>
    public long? RelatedEntityId { get; set; }

    /// <summary>
    /// 关联实体快照（JSON 格式记录关键信息，便于审计追溯）
    /// </summary>
    public string? RelatedEntitySnapshot { get; set; }

    /// <summary>
    /// 转出客户ID（仅疗程卡转让使用）
    /// </summary>
    public long? FromCustomerId { get; set; }

    /// <summary>
    /// 转入客户ID（仅疗程卡转让使用）
    /// </summary>
    public long? ToCustomerId { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}
