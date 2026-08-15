namespace Bms.Store.Domain.Entities;

/// <summary>
/// 服务项目-设备类型 关联表
/// 对应需求 B7.2：服务项目关联"需要的设备类型"（而非具体设备实例，由预约时动态分配）
/// </summary>
public class ServiceProductEquipment : StoreBusinessEntityBase
{
    /// <summary>
    /// 服务项目子表 ID（关联 ServiceProduct.Id）
    /// </summary>
    public long ServiceProductId { get; set; }

    /// <summary>
    /// 设备类型 ID（关联 EquipmentType.Id）
    /// </summary>
    public long EquipmentTypeId { get; set; }

    /// <summary>
    /// 导航属性：服务项目
    /// </summary>
    public ServiceProduct? ServiceProduct { get; set; }

    /// <summary>
    /// 导航属性：设备类型
    /// </summary>
    public EquipmentType? EquipmentType { get; set; }
}
