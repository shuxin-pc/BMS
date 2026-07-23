namespace Bms.Store.Application.Dtos.PurchaseOrders;

/// <summary>
/// 更新采购订单明细DTO
/// </summary>
public class PurchaseOrderItemUpdateDto : PurchaseOrderItemCreateDto
{
    public long Id { get; set; }
}
