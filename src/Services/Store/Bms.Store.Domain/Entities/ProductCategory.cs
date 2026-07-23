namespace Bms.Store.Domain.Entities;

/// <summary>
/// 商品分类
/// </summary>
public class ProductCategory : StoreEntity
{
    /// <summary>
    /// 分类名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 分类编码（可选，前端不传时由后端处理）
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    /// 父级ID
    /// </summary>
    public long? ParentId { get; set; }

    /// <summary>
    /// 排序
    /// </summary>
    public int Sort { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}
