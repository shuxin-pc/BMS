namespace Bms.Store.Application.Dtos.ServiceBoms;

/// <summary>
/// 耗材商品轻量选项（用于BOM下拉选择）
/// </summary>
public class ConsumableOptionDto
{
    /// <summary>耗材商品ID（Product.Id）</summary>
    public long Id { get; set; }

    /// <summary>耗材商品名称（来自 ProductMaster.Name）</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>耗材商品编码（来自 ProductMaster.Code）</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>耗材单位（来自 ProductMaster.Unit）</summary>
    public string? Unit { get; set; }
}
