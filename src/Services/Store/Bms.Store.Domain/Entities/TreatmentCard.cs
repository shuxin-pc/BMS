namespace Bms.Store.Domain.Entities;

/// <summary>
/// 项目卡配置
/// </summary>
public class TreatmentCard : StoreEntity
{
    /// <summary>
    /// 卡名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 包含项目描述
    /// </summary>
    public string? ServiceItems { get; set; }

    /// <summary>
    /// 总次数
    /// </summary>
    public int TotalTimes { get; set; }

    /// <summary>
    /// 单价
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// 有效期（天）
    /// </summary>
    public int ValidityDays { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}
