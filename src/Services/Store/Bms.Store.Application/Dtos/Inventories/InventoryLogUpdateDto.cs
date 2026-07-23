namespace Bms.Store.Application.Dtos.Inventories;

/// <summary>
/// 更新库存流水输入 DTO
/// </summary>
public class InventoryLogUpdateDto : InventoryLogCreateDto
{
    public long Id { get; set; }
}
