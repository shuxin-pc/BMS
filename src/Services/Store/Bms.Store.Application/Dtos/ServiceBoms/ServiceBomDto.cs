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
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
