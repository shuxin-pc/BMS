namespace Bms.Store.Application.Dtos.PurchaseOrders;

/// <summary>
/// 更新采购订单DTO
/// </summary>
public class PurchaseOrderUpdateDto : PurchaseOrderCreateDto
{
    public long Id { get; set; }
}
