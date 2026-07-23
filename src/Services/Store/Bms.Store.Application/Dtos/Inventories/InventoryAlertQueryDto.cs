using Bms.Store.Application.Dtos;

namespace Bms.Store.Application.Dtos.Inventories;

/// <summary>
/// 库存预警分页查询参数
/// </summary>
public class InventoryAlertQueryDto : PagedRequestDto
{
    public long? ProductId { get; set; }
    public int? AlertType { get; set; }
    public bool? IsProcessed { get; set; }
}
