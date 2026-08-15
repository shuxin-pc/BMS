namespace Bms.Store.Domain.Entities;

/// <summary>
/// 服务对比照片明细
/// 一次服务的服务前/服务后可拍摄多张照片（如正面、侧面），每张一条明细
/// </summary>
public class ServiceComparisonPhotoItem : StoreBusinessEntityBase
{
    /// <summary>
    /// 所属对比照片记录ID
    /// </summary>
    public long ServiceComparisonPhotoId { get; set; }

    /// <summary>
    /// 照片来源：对象存储的 objectKey（如 1001/2001/customer-photo/202608/7234.jpg）
    /// 或用户录入的外部图片直链（http/https 开头）。
    /// 不存完整 URL —— 预签名 URL 会过期，静态拼接的地址在 private 桶下访问不通，
    /// 且写死 endpoint 会导致换域名时历史数据全废。展示地址一律读取时现场签发
    /// </summary>
    public string PhotoSource { get; set; } = string.Empty;

    /// <summary>
    /// 展示排序号（同一记录内从 0 递增，决定多张照片的展示先后）
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// 导航属性：所属对比照片记录
    /// </summary>
    public ServiceComparisonPhoto? ServiceComparisonPhoto { get; set; }
}
