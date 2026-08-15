namespace Bms.Store.Domain.Entities;

/// <summary>
/// 服务前后对比照片
/// 关联订单ID，记录客户服务前后的照片对比
/// 一条记录代表某次服务的"服务前"或"服务后"，其下可挂多张照片（见 Items）
/// </summary>
public class ServiceComparisonPhoto : StoreBusinessEntityBase
{
    /// <summary>
    /// 客户ID
    /// </summary>
    public long CustomerId { get; set; }

    /// <summary>
    /// 关联订单ID（选填，补录历史照片时可能已无法对应到具体订单）
    /// </summary>
    public long? OrderId { get; set; }

    /// <summary>
    /// 服务项目商品ID（引用门店商品档案 Product.Id，仅服务类商品）
    /// </summary>
    public long? ProductId { get; set; }

    /// <summary>
    /// 服务项目名称快照（拍照时从商品主档复制并锁定）
    /// 同时作为前后照片配对的分组键之一，商品改名不影响既有配对关系
    /// </summary>
    public string? ServiceItem { get; set; }

    /// <summary>
    /// 拍照日期
    /// </summary>
    public DateTime PhotoDate { get; set; }

    /// <summary>
    /// 照片类型（1:服务前 2:服务后）
    /// </summary>
    public int PhotoType { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 导航属性：客户
    /// </summary>
    public Customer? Customer { get; set; }

    /// <summary>
    /// 导航属性：照片明细（同一次服务的同一类型可拍摄多张照片）
    /// </summary>
    public List<ServiceComparisonPhotoItem> Items { get; set; } = new();
}
