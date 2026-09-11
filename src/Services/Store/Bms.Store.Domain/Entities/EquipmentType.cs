namespace Bms.Store.Domain.Entities;

/// <summary>
/// 设备类型
/// 描述一类设备的规格/型号，与具体实例 Equipment 一对多
/// 服务项目(ServiceProduct)关联设备类型，预约(Appointment)时才绑定具体实例
/// 支持父子级自引用：父级节点作为"分类/分组"（如"激光类"），子级节点为具体型号（如"飞顿激光"）
/// </summary>
public class EquipmentType : StoreEntity
{
    /// <summary>
    /// 类型名称（如"激光类"、"飞顿激光"、"热玛吉"）
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 类型编码（租户+门店范围内唯一，用于程序化引用）
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 父级类型ID（null=顶级分类/分组；非 null 为挂在该分类下的具体型号）
    /// </summary>
    public long? ParentId { get; set; }

    /// <summary>
    /// 导航属性：父级类型
    /// </summary>
    public EquipmentType? Parent { get; set; }

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
