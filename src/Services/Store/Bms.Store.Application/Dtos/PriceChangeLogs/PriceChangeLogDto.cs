namespace Bms.Store.Application.Dtos.PriceChangeLogs;

/// <summary>
/// 价格变更记录输出 DTO
/// </summary>
public class PriceChangeLogDto
{
    public long Id { get; set; }
    public long ProductId { get; set; }
    public decimal OldPrice { get; set; }
    public decimal NewPrice { get; set; }
    public DateTime ChangeTime { get; set; }
    public long? OperatorId { get; set; }
    public string? Remark { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
