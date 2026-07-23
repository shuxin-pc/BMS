namespace Bms.Store.Application.Dtos.Inventories;

/// <summary>
/// 库存盘点记录输出 DTO
/// </summary>
public class InventoryCheckDto
{
    public long Id { get; set; }
    public long ProductId { get; set; }
    public decimal BeforeQuantity { get; set; }
    public decimal ActualQuantity { get; set; }
    public decimal DiffQuantity { get; set; }
    public DateTime CheckTime { get; set; }
    public long? OperatorId { get; set; }

    /// <summary>
    /// 盘点单状态（0=草稿 1=已完成 2=已取消）
    /// </summary>
    public int Status { get; set; }

    public string? Remark { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
