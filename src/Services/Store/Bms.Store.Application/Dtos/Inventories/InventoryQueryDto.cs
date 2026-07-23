using Bms.Store.Application.Dtos;

namespace Bms.Store.Application.Dtos.Inventories;

/// <summary>
/// 库存分页查询参数
/// </summary>
public class InventoryQueryDto : PagedRequestDto
{
    public long? ProductId { get; set; }
}
