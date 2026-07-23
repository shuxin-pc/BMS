namespace Bms.Store.Application.Dtos.TreatmentCards;

/// <summary>
/// 疗程卡配置 DTO
/// </summary>
public class TreatmentCardDto
{
    public long Id { get; set; }

    /// <summary>
    /// 归属门店ID（可空，疗程卡在租户内跨店通用）
    /// </summary>
    public long? StoreId { get; set; }

    /// <summary>
    /// 归属门店编码
    /// </summary>
    public string? StoreCode { get; set; }

    /// <summary>
    /// 卡名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 卡编码
    /// </summary>
    public string Code { get; set; } = string.Empty;

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
    public bool IsEnabled { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 包含的项目明细列表（按项目原价比例分摊折算单价）
    /// </summary>
    public List<CourseCardItemDto> Items { get; set; } = new();

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
