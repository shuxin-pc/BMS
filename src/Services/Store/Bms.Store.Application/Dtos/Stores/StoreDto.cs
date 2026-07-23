namespace Bms.Store.Application.Dtos.Stores;

/// <summary>
/// 门店详情 DTO（对齐前端 Store 接口契约）
/// </summary>
public class StoreDto
{
    /// <summary>
    /// 门店ID
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 门店名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 门店编码（业务编码，如 S001）
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 门店简称
    /// </summary>
    public string? ShortName { get; set; }

    /// <summary>
    /// 联系电话
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// 门店地址
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// 营业时间（如 "09:00 - 22:00"）
    /// </summary>
    public string? BusinessHours { get; set; }

    /// <summary>
    /// 门店面积（平方米）
    /// </summary>
    public decimal? Area { get; set; }

    /// <summary>
    /// 店长姓名
    /// </summary>
    public string? ManagerName { get; set; }

    /// <summary>
    /// 门店状态：1-营业，2-歇业
    /// </summary>
    public int Status { get; set; }

    /// <summary>
    /// 门店logo图片URL
    /// </summary>
    public string? LogoUrl { get; set; }

    /// <summary>
    /// 营业执照图片URL
    /// </summary>
    public string? BusinessLicenseUrl { get; set; }

    /// <summary>
    /// 门店描述/备注
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}
