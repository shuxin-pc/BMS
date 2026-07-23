namespace Bms.Store.Domain.Entities;

/// <summary>
/// 服务项目物料清单（BOM）
/// 记录服务项目所需耗材及数量，用于服务完成时自动扣减耗材库存
/// 替代原 ServiceProduct.Consumables 的 JSON 存储方式
/// </summary>
public class ServiceBom : StoreEntity
{
    /// <summary>
    /// 服务项目ID（关联 ServiceProduct.Id）
    /// </summary>
    public long ServiceProductId { get; set; }

    /// <summary>
    /// 耗材商品ID（关联 Product.Id，耗材的品项主表ID）
    /// </summary>
    public long ConsumableProductId { get; set; }

    /// <summary>
    /// 单次服务消耗数量
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// 导航属性：服务项目
    /// </summary>
    public ServiceProduct? ServiceProduct { get; set; }

    /// <summary>
    /// 导航属性：耗材商品
    /// </summary>
    public Product? Product { get; set; }
}
