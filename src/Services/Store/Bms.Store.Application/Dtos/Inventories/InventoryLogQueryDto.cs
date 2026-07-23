using Bms.Store.Application.Dtos;

namespace Bms.Store.Application.Dtos.Inventories;

/// <summary>
/// 库存流水分页查询参数
/// </summary>
public class InventoryLogQueryDto : PagedRequestDto
{
    public long? ProductId { get; set; }

    /// <summary>
    /// 操作类型（1:入库 2:出库）
    /// </summary>
    public int? Type { get; set; }

    /// <summary>
    /// 来源类型（1:采购入库 2:退货入库 3:盘点调整 4:调拨入库 5:调拨出库 6:其他）
    /// </summary>
    public int? SourceType { get; set; }
}
