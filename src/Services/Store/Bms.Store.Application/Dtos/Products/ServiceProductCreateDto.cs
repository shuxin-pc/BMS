namespace Bms.Store.Application.Dtos.Products;

/// <summary>
/// 创建服务商品子表输入 DTO
/// </summary>
public class ServiceProductCreateDto
{
    public long ProductId { get; set; }
    public int? Duration { get; set; }
    public int? RequiredRoomType { get; set; }

    /// <summary>
    /// 所需设备类型 ID 列表
    /// </summary>
    public List<long> EquipmentTypeIds { get; set; } = new();

    public string? ApplicableSkills { get; set; }
}
