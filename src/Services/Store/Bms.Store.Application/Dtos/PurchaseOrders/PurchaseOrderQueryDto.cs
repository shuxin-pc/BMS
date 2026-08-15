using Bms.Store.Application.Dtos;

namespace Bms.Store.Application.Dtos.PurchaseOrders;

/// <summary>
/// 采购订单分页查询DTO
/// </summary>
public class PurchaseOrderQueryDto : PagedRequestDto
{
    public string? OrderNo { get; set; }
    public long? SupplierId { get; set; }
    public long? ProductId { get; set; }
    public int? PurchaseType { get; set; }

    /// <summary>
    /// 采购日期开始（含当天，OrderDate >= OrderDateStart）
    /// </summary>
    public DateTime? OrderDateStart { get; set; }

    /// <summary>
    /// 采购日期结束（含当天，OrderDate &lt; OrderDateEnd.AddDays(1)）
    /// </summary>
    public DateTime? OrderDateEnd { get; set; }
}
