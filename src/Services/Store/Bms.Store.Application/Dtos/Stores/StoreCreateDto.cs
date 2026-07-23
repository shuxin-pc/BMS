namespace Bms.Store.Application.Dtos.Stores;

/// <summary>
/// 创建门店请求 DTO
/// </summary>
public class StoreCreateDto
{
    /// <summary>
    /// 门店名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 门店编码
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
    /// 营业时间
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
    public int Status { get; set; } = 1;

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
}
