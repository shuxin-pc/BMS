namespace Bms.Store.Application.Dtos.Products;

/// <summary>
/// 商品输出 DTO（对齐前端 Product 契约）
/// 商品多态模型：主表区分类型，仅服务项目（Type=2）有子表字段：
/// - Type=2 服务项目：Duration、RequiredRoomType、EquipmentIds/EquipmentNames、SkillCategoryIds/SkillCategoryNames
/// </summary>
public class ProductDto
{
    public long Id { get; set; }
    /// <summary>关联商品主档ID</summary>
    public long MasterId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public long CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public int Type { get; set; }
    public string? Spec { get; set; }
    public string? Unit { get; set; }
    /// <summary>品牌</summary>
    public string? Brand { get; set; }
    /// <summary>默认供应商ID（从 ProductSupplier.IsDefault=true 派生）</summary>
    public long? DefaultSupplierId { get; set; }
    /// <summary>默认供应商名称</summary>
    public string? DefaultSupplierName { get; set; }
    public decimal Price { get; set; }
    public decimal? CostPrice { get; set; }
    /// <summary>上次采购价（采购入库时自动更新，分店独立采购）</summary>
    public decimal? LastPurchasePrice { get; set; }
    public decimal? LowStockThreshold { get; set; }
    public int? ExpiryAlertDays { get; set; }
    public decimal? OverstockThreshold { get; set; }
    public string? ImageUrl { get; set; }
    public int Status { get; set; }
    /// <summary>是否可销售（样品/赠品为 false，不可通过 POS 销售下单）</summary>
    public bool IsSalable { get; set; }
    /// <summary>分店级备注</summary>
    public string? Remark { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // ========== 服务项目子表字段（Type=2）==========
    /// <summary>服务时长（分钟，服务项目）</summary>
    public int? Duration { get; set; }
    /// <summary>所需房间/床位类型（1:房间 2:床位，null=不限，服务项目）</summary>
    public int? RequiredRoomType { get; set; }
    /// <summary>所需设备类型 ID 列表（服务项目）</summary>
    public List<long> EquipmentTypeIds { get; set; } = new();
    /// <summary>所需设备类型名称列表（服务项目，展示用）</summary>
    public List<string> EquipmentTypeNames { get; set; } = new();
    /// <summary>适用技师技能分类 ID 列表（服务项目）</summary>
    public List<long> SkillCategoryIds { get; set; } = new();
    /// <summary>适用技师技能分类名称列表（服务项目，展示用）</summary>
    public List<string> SkillCategoryNames { get; set; } = new();
}
