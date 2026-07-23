namespace Bms.Store.Application.Dtos.Inventories;

/// <summary>
/// 创建库存盘点记录输入 DTO
/// </summary>
public class InventoryCheckCreateDto
{
    public long ProductId { get; set; }
    public decimal BeforeQuantity { get; set; }
    public decimal ActualQuantity { get; set; }
    public decimal DiffQuantity { get; set; }
    public DateTime CheckTime { get; set; }
    public long? OperatorId { get; set; }
    public string? Remark { get; set; }
}
