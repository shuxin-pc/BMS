namespace Bms.Store.Application.Dtos.Products;

/// <summary>
/// 创建商品输入 DTO
/// 商品多态模型：主表区分类型，仅服务项目（Type=2）有子表字段：
/// - Type=2 服务项目：Duration、RequiredRoomType、EquipmentIds、ApplicableSkills
/// </summary>
public class ProductCreateDto
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public long CategoryId { get; set; }
    public int Type { get; set; }
    public string? Spec { get; set; }
    public string? Unit { get; set; }
    /// <summary>品牌</summary>
    public string? Brand { get; set; }
    /// <summary>供应商ID</summary>
    public long? SupplierId { get; set; }
    public decimal Price { get; set; }
    public decimal? CostPrice { get; set; }
    /// <summary>低库存预警阈值（null=不预警）</summary>
    public decimal? LowStockThreshold { get; set; }
    /// <summary>效期预警天数（null=不预警）</summary>
    public int? ExpiryAlertDays { get; set; }
    /// <summary>积压预警阈值（null=不预警）</summary>
    public decimal? OverstockThreshold { get; set; }
    public string? ImageUrl { get; set; }
    public int Status { get; set; }
    public string? Description { get; set; }

    // ========== 服务项目子表字段（Type=2）==========
    /// <summary>服务时长（分钟，服务项目）</summary>
    public int? Duration { get; set; }
    /// <summary>所需房间/床位类型（1:房间 2:床位，null=不限，服务项目）</summary>
    public int? RequiredRoomType { get; set; }
    /// <summary>所需设备类型 ID 列表（服务项目）</summary>
    public List<long> EquipmentTypeIds { get; set; } = new();
    /// <summary>适用技师技能标签（服务项目）</summary>
    public string? ApplicableSkills { get; set; }
}
