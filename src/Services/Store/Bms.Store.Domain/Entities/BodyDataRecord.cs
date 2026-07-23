namespace Bms.Store.Domain.Entities;

/// <summary>
/// 客户身体数据记录
/// 按时间序列记录身体数据，支持趋势查询
/// </summary>
public class BodyDataRecord : StoreBusinessEntityBase
{
    /// <summary>
    /// 客户ID
    /// </summary>
    public long CustomerId { get; set; }

    /// <summary>
    /// 记录日期
    /// </summary>
    public DateTime RecordDate { get; set; }

    /// <summary>
    /// 体重(kg)
    /// </summary>
    public decimal? Weight { get; set; }

    /// <summary>
    /// 体脂率(%)
    /// </summary>
    public decimal? BodyFat { get; set; }

    /// <summary>
    /// 胸围(cm)
    /// </summary>
    public decimal? Bust { get; set; }

    /// <summary>
    /// 腰围(cm)
    /// </summary>
    public decimal? Waist { get; set; }

    /// <summary>
    /// 臀围(cm)
    /// </summary>
    public decimal? Hip { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 导航属性：客户
    /// </summary>
    public Customer? Customer { get; set; }
}
