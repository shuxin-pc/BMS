namespace Bms.Store.Application.Dtos.ServiceBoms;

/// <summary>
/// 服务BOM输出 DTO
/// </summary>
public class ServiceBomDto
{
    public long Id { get; set; }
    public long ServiceProductId { get; set; }
    public long ConsumableProductId { get; set; }
    public decimal Quantity { get; set; }

    /// <summary>服务项目名称（来自 ServiceProduct.Master.Name，冗余展示用）</summary>
    public string ServiceProductName { get; set; } = string.Empty;

    /// <summary>耗材商品名称（来自 Product.Master.Name，冗余展示用）</summary>
    public string ConsumableProductName { get; set; } = string.Empty;

    /// <summary>耗材商品编码（来自 Product.Master.Code，冗余展示用）</summary>
    public string? ConsumableProductCode { get; set; }

    /// <summary>耗材单位（来自 Product.Master.Unit，冗余展示用）</summary>
    public string? Unit { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
