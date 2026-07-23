using Bms.Store.Application.Dtos;

namespace Bms.Store.Application.Dtos.PurchaseOrders;

/// <summary>
/// 采购订单明细分页查询DTO
/// </summary>
public class PurchaseOrderItemQueryDto : PagedRequestDto
{
    public long? PurchaseOrderId { get; set; }
    public long? ProductId { get; set; }
}
