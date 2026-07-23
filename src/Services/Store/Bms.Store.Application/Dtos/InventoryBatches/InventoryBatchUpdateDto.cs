namespace Bms.Store.Application.Dtos.InventoryBatches;

/// <summary>
/// 更新库存批次输入 DTO
/// </summary>
public class InventoryBatchUpdateDto : InventoryBatchCreateDto
{
    public long Id { get; set; }
}
