namespace Bms.Store.Domain.Entities;

/// <summary>
/// 客户历史服务反应
/// 动态跟踪客户每次服务的身体反应，用于服务调整参考
/// </summary>
public class ServiceReaction : StoreBusinessEntityBase
{
    /// <summary>
    /// 客户ID
    /// </summary>
    public long CustomerId { get; set; }

    /// <summary>
    /// 关联订单ID
    /// </summary>
    public long? OrderId { get; set; }

    /// <summary>
    /// 服务项目商品ID（引用门店商品档案 Product.Id，仅服务类商品）
    /// 可空：客户通过电话/微信反馈时可能无法确定具体项目，此时仅保留文字描述
    /// 用于不良反应率统计与追溯服务所用耗材（排查过敏源）
    /// </summary>
    public long? ProductId { get; set; }

    /// <summary>
    /// 服务项目名称快照（登记时从商品主档复制并锁定）
    /// 商品改名或下架后档案仍保持当时的项目名，保证纠纷举证时不失真
    /// </summary>
    public string? ServiceItem { get; set; }

    /// <summary>
    /// 反应日期
    /// </summary>
    public DateTime ReactionDate { get; set; }

    /// <summary>
    /// 服务反应描述
    /// </summary>
    public string? Reaction { get; set; }

    /// <summary>
    /// 严重程度（1:轻微 2:中等 3:严重）
    /// </summary>
    public int? Severity { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 导航属性：客户
    /// </summary>
    public Customer? Customer { get; set; }
}
