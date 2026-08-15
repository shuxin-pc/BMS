namespace Bms.Store.Application.Dtos.Customers;

/// <summary>
/// 服务对比照片明细（返回用）
/// </summary>
public class ServiceComparisonPhotoItemDto
{
    public long Id { get; set; }
    /// <summary>
    /// 可展示 URL（对象存储的预签名 URL 或原样返回的外部图片直链）
    /// </summary>
    public string PhotoUrl { get; set; } = string.Empty;
    /// <summary>
    /// 展示排序号
    /// </summary>
    public int SortOrder { get; set; }
}

/// <summary>
/// 服务对比照片明细（提交用）
/// 编辑时已有照片仅回传 Id 即可保留，无需重新上传
/// </summary>
public class ServiceComparisonPhotoItemSaveDto
{
    /// <summary>
    /// 明细ID：有值表示保留已有照片，为空表示新增照片
    /// </summary>
    public long? Id { get; set; }
    /// <summary>
    /// 照片来源：上传接口返回的 objectKey，或用户录入的外部图片直链
    /// （新增照片时必填；保留已有照片时忽略）
    /// </summary>
    public string? ObjectKey { get; set; }
}
