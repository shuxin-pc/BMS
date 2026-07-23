namespace Bms.Store.Domain.Entities;

/// <summary>
/// 客户美容档案
/// 美容行业扩展：肤质/发质、过敏记录、身体数据、服务前后对比
/// </summary>
public class CustomerBeautyProfile : StoreEntity
{
    /// <summary>
    /// 客户ID
    /// </summary>
    public long CustomerId { get; set; }

    /// <summary>
    /// 肤质类型（如：干性、油性、混合性、敏感性）
    /// </summary>
    public string? SkinType { get; set; }

    /// <summary>
    /// 敏感程度
    /// </summary>
    public string? Sensitivity { get; set; }

    /// <summary>
    /// 发质情况
    /// </summary>
    public string? HairType { get; set; }

    /// <summary>
    /// 过敏史和禁忌成分
    /// </summary>
    public string? AllergyHistory { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 导航属性：客户
    /// </summary>
    public Customer? Customer { get; set; }
}
