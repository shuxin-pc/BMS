namespace Bms.Store.Application.Dtos.Products;

/// <summary>
/// 更新商品主档输入 DTO
/// Master 字段修改全租户生效（设计文档 6.2 节）
/// </summary>
public class ProductMasterUpdateDto
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Type { get; set; }
    public long CategoryId { get; set; }
    public string? Unit { get; set; }
    public string? Specification { get; set; }
    public string? Brand { get; set; }
    public string? ImageUrl { get; set; }
    public string? Remark { get; set; }

    // ========== 服务项目子表字段（Type=2）==========
    public int? Duration { get; set; }
    public int? RequiredRoomType { get; set; }
    public List<long> EquipmentTypeIds { get; set; } = new();
    public List<long> SkillCategoryIds { get; set; } = new();
}
