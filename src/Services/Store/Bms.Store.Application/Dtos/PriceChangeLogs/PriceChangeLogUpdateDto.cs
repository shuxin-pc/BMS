namespace Bms.Store.Application.Dtos.PriceChangeLogs;

/// <summary>
/// 更新价格变更记录输入 DTO
/// </summary>
public class PriceChangeLogUpdateDto : PriceChangeLogCreateDto
{
    public long Id { get; set; }
}
