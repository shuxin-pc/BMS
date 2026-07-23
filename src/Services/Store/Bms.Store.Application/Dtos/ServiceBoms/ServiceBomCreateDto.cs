namespace Bms.Store.Application.Dtos.ServiceBoms;

/// <summary>
/// 创建服务BOM输入 DTO
/// </summary>
public class ServiceBomCreateDto
{
    public long ServiceProductId { get; set; }
    public long ConsumableProductId { get; set; }
    public decimal Quantity { get; set; }
}
