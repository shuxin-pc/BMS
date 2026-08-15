namespace Bms.Store.Application.Dtos.Products;

/// <summary>
/// 创建商品主档输入 DTO
/// Master 字段全租户生效，门店档案通过 BatchConfigStoreFieldsAsync 单独配置
/// </summary>
public class ProductMasterCreateDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    /// <summary>商品类型（1:实物 2:服务 3:耗材 4:样品 5:赠品）</summary>
    public int Type { get; set; }
    public long CategoryId { get; set; }
    public string? Unit { get; set; }
    public string? Specification { get; set; }
    public string? Brand { get; set; }
    public string? ImageUrl { get; set; }
    public string? Remark { get; set; }

    // ========== 服务项目子表字段（Type=2）==========
    /// <summary>服务时长（分钟，服务项目）</summary>
    public int? Duration { get; set; }
    /// <summary>所需房间/床位类型（1:房间 2:床位，null=不限，服务项目）</summary>
    public int? RequiredRoomType { get; set; }
    /// <summary>所需设备类型 ID 列表（服务项目）</summary>
    public List<long> EquipmentTypeIds { get; set; } = new();
    /// <summary>适用技师技能分类 ID 列表（服务项目，树形选择，选父级即覆盖其所有子级）</summary>
    public List<long> SkillCategoryIds { get; set; } = new();
}
