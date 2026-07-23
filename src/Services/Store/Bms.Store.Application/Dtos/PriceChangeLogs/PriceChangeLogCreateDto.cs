namespace Bms.Store.Application.Dtos.PriceChangeLogs;

/// <summary>
/// 创建价格变更记录输入 DTO
/// </summary>
public class PriceChangeLogCreateDto
{
    public long ProductId { get; set; }
    public decimal OldPrice { get; set; }
    public decimal NewPrice { get; set; }
    public DateTime ChangeTime { get; set; }
    public long? OperatorId { get; set; }
    public string? Remark { get; set; }
}
