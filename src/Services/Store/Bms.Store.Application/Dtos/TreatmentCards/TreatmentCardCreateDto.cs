namespace Bms.Store.Application.Dtos.TreatmentCards;

/// <summary>
/// 创建项目卡配置 DTO
/// </summary>
public class TreatmentCardCreateDto
{
    /// <summary>
    /// 归属门店ID（可空，项目卡在租户内跨店通用）
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

    /// <summary>
    /// 包含的项目明细列表
    /// 后端根据各项目原价 × 次数的比例分摊卡价（Price），计算并锁定折算单价
    /// </summary>
    public List<CourseCardItemCreateDto> Items { get; set; } = new();
}
