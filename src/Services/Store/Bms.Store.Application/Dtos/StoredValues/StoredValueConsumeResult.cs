namespace Bms.Store.Application.Dtos.StoredValues;

/// <summary>
/// 储值消费结果 DTO（供 OrderAppService.CreateAsync 在事务内调用 StoredValueAccountAppService.ConsumeAsync 时使用）
/// 不直接对外暴露，仅用于内部服务间通信
/// </summary>
public class StoredValueConsumeResult
{
    /// <summary>
    /// 是否扣减成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 失败时的错误信息（Success=false 时填充）
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 实际扣减的实收余额
    /// </summary>
    public decimal RealDeduct { get; set; }

    /// <summary>
    /// 实际扣减的赠送余额
    /// </summary>
    public decimal GiftDeduct { get; set; }

    /// <summary>
    /// 扣减前账户总余额
    /// </summary>
    public decimal BeforeBalance { get; set; }

    /// <summary>
    /// 扣减后账户总余额
    /// </summary>
    public decimal AfterBalance { get; set; }
}
