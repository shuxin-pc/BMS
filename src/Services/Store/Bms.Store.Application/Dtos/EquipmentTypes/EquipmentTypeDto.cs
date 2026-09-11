namespace Bms.Store.Application.Dtos.EquipmentTypes;

/// <summary>
/// 设备类型输出 DTO
/// </summary>
public class EquipmentTypeDto
{
    public long Id { get; set; }

    /// <summary>
    /// 类型名称（如"飞顿激光"、"热玛吉"）
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 类型编码（租户内唯一）
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 父级类型ID（null=顶级分类/分组）
    /// </summary>
    public long? ParentId { get; set; }

    /// <summary>
    /// 父级类型名称（冗余展示字段）
    /// </summary>
    public string? ParentName { get; set; }

    /// <summary>
    /// 规格/型号
    /// </summary>
    public string? Spec { get; set; }

    /// <summary>
    /// 备注说明
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 是否启用（停用后设备档案不可再选择该类型）
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// 关联的设备实例数量（租户内，用于列表展示与删除提示）
    /// </summary>
    public int EquipmentCount { get; set; }

    /// <summary>
    /// 子级类型列表（树形结构，GetTree 使用）
    /// </summary>
    public List<EquipmentTypeDto>? Children { get; set; }

    public DateTime CreatedTime { get; set; }
}
