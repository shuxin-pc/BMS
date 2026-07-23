namespace Bms.Store.Application.Dtos.StoredValues;

/// <summary>
/// 储值规则 DTO
/// </summary>
public class StoredValueRuleDto
{
    public long Id { get; set; }

    /// <summary>
    /// 规则名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 规则编码
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 充值金额
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// 赠送金额
    /// </summary>
    public decimal GiftAmount { get; set; }

    /// <summary>
    /// 赠送比例
    /// </summary>
    public decimal? GiftRate { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// 排序
    /// </summary>
    public int Sort { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
