namespace Bms.Store.Domain.Entities;

/// <summary>
/// 商品分类（租户级共享，对应设计文档 3.3 节）
/// 移除门店隔离，统一管理便于跨店报表口径一致
/// </summary>
public class ProductCategory : StoreTenantEntity
{
    /// <summary>
    /// 分类名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 分类编码
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    /// 父级ID
    /// </summary>
    public long? ParentId { get; set; }
}
