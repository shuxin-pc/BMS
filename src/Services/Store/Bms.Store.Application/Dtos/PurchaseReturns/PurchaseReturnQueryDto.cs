using Bms.Store.Application.Dtos;

namespace Bms.Store.Application.Dtos.PurchaseReturns;

/// <summary>
/// 采购退货分页查询参数
/// </summary>
public class PurchaseReturnQueryDto : PagedRequestDto
{
    public string? ReturnNo { get; set; }
    public long? SupplierId { get; set; }
}
