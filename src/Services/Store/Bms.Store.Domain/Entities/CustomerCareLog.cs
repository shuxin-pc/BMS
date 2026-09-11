namespace Bms.Store.Domain.Entities;

/// <summary>
/// 客户关怀类型常量
/// 1=生日关怀, 2=消费感谢
/// </summary>
public static class CustomerCareLogTypes
{
    public const int BirthdayCare = 1;
    public const int ConsumeThank = 2;
}

/// <summary>
/// 客户关怀流水记录（无软删除，流水表）
/// 记录每次生日关怀/消费感谢操作历史，状态通过是否存在记录实时计算
/// </summary>
public class CustomerCareLog : StoreBusinessEntityBase
{
    /// <summary>
    /// 客户ID
    /// </summary>
    public long CustomerId { get; set; }

    /// <summary>
    /// 关怀类型（1=生日关怀 2=消费感谢）
    /// </summary>
    public int Type { get; set; }

    /// <summary>
    /// 消费感谢关联订单ID（Type=2 时必填）
    /// </summary>
    public long? RefOrderId { get; set; }

    /// <summary>
    /// 生日关怀年份（Type=1 时必填，用于"每年一条"幂等）
    /// </summary>
    public int? CareYear { get; set; }

    /// <summary>
    /// 感谢方式 1=短信 2=微信 3=电话（Type=2 时必填）
    /// </summary>
    public int? Method { get; set; }

    /// <summary>
    /// 操作人姓名（冗余展示，来自 ICurrentUser.RealName/UserName，列表展示识别谁完成的关怀/感谢）
    /// </summary>
    public string? OperatorName { get; set; }

    /// <summary>
    /// 关怀/感谢时间
    /// </summary>
    public DateTime CareTime { get; set; }

    /// <summary>
    /// 导航属性：客户
    /// </summary>
    public Customer? Customer { get; set; }
}
