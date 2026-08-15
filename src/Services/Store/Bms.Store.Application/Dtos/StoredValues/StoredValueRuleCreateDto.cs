namespace Bms.Store.Application.Dtos.StoredValues;

/// <summary>
/// 创建储值规则 DTO
/// </summary>
public class StoredValueRuleCreateDto
{
    /// <summary>
    /// 规则名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 充值金额（同租户内唯一）
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// 赠送金额
    /// </summary>
    public decimal GiftAmount { get; set; }

    /// <summary>
    /// 生效日期（含当天）
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// 失效日期（含当天，null 表示长期有效）
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 排序
    /// </summary>
    public int Sort { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}
