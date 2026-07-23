namespace Bms.Store.Domain.Entities;

/// <summary>
/// 客户偏好
/// 记录客户服务偏好（手法力度、温度、音乐、常用技师）
/// </summary>
public class CustomerPreference : StoreEntity
{
    /// <summary>
    /// 客户ID
    /// </summary>
    public long CustomerId { get; set; }

    /// <summary>
    /// 手法力度偏好（轻/中/重）
    /// </summary>
    public string? TechniquePressure { get; set; }

    /// <summary>
    /// 温度偏好
    /// </summary>
    public string? Temperature { get; set; }

    /// <summary>
    /// 音乐偏好
    /// </summary>
    public string? MusicPreference { get; set; }

    /// <summary>
    /// 常用技师ID
    /// </summary>
    public long? PreferredTechnicianId { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 导航属性：客户
    /// </summary>
    public Customer? Customer { get; set; }
}
