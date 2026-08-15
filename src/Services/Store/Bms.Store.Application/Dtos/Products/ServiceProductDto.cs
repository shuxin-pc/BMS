namespace Bms.Store.Application.Dtos.Products;

/// <summary>
/// 服务商品子表输出 DTO
/// </summary>
public class ServiceProductDto
{
    public long Id { get; set; }
    public long ProductId { get; set; }
    public int? Duration { get; set; }
    public int? RequiredRoomType { get; set; }

    /// <summary>
    /// 所需设备类型 ID 列表
    /// </summary>
    public List<long> EquipmentTypeIds { get; set; } = new();

    /// <summary>
    /// 所需设备类型名称列表（展示用，由服务层填充）
    /// </summary>
    public List<string> EquipmentTypeNames { get; set; } = new();

    /// <summary>
    /// 适用技师技能分类 ID 列表（由服务层填充）
    /// </summary>
    public List<long> SkillCategoryIds { get; set; } = new();

    /// <summary>
    /// 适用技师技能分类名称列表（展示用，由服务层填充）
    /// </summary>
    public List<string> SkillCategoryNames { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
