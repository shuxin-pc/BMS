namespace Bms.Store.Application.Dtos.EquipmentTypes;

/// <summary>
/// 新建设备类型输入 DTO
/// </summary>
public class EquipmentTypeCreateDto
{
    /// <summary>
    /// 类型名称（如"飞顿激光"、"热玛吉"）
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 类型编码（租户内唯一，用于程序化引用）
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 父级类型ID（null=顶级分类/分组）
    /// </summary>
    public long? ParentId { get; set; }

    /// <summary>
    /// 规格/型号
    /// </summary>
    public string? Spec { get; set; }

    /// <summary>
    /// 备注说明
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 是否启用（默认启用）
    /// </summary>
    public bool IsActive { get; set; } = true;
}
