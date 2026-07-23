namespace Bms.Store.Application.Dtos.Statistics;

/// <summary>
/// 商品销售统计输出 DTO
/// </summary>
public class ProductSalesStatDto
{
    public long Id { get; set; }
    public DateTime StatDate { get; set; }
    public string StatMonth { get; set; } = string.Empty;
    public long ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int ProductType { get; set; }
    public int SalesCount { get; set; }
    public decimal SalesAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
