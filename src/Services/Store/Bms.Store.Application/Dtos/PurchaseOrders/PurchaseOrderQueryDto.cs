using Bms.Store.Application.Dtos;

namespace Bms.Store.Application.Dtos.PurchaseOrders;

/// <summary>
/// 采购订单分页查询DTO
/// </summary>
public class PurchaseOrderQueryDto : PagedRequestDto
{
    public string? OrderNo { get; set; }
    public long? SupplierId { get; set; }
    public int? Status { get; set; }
    public int? PurchaseType { get; set; }
}
