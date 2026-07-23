namespace Bms.Store.Domain.Entities;

/// <summary>
/// 设备类型
/// 描述一类设备的规格/型号，与具体实例 Equipment 一对多
/// 服务项目(ServiceProduct)关联设备类型，预约(Appointment)时才绑定具体实例
/// </summary>
public class EquipmentType : StoreEntity
{
    /// <summary>
    /// 类型名称（如"飞顿激光"、"热玛吉"）
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 类型编码（租户+门店范围内唯一，用于程序化引用）
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 分类（如"激光类"、"射频类"、"注射类"）
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// 规格/型号
    /// </summary>
    public string? Spec { get; set; }

    /// <summary>
    /// 备注说明
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 是否启用（停用后新建服务项目不能选择该类型）
    /// </summary>
    public bool IsActive { get; set; } = true;
}
