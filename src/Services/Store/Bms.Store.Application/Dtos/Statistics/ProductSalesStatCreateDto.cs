namespace Bms.Store.Application.Dtos.Statistics;

/// <summary>
/// 创建商品销售统计输入 DTO
/// </summary>
public class ProductSalesStatCreateDto
{
    public DateTime StatDate { get; set; }
    public string StatMonth { get; set; } = string.Empty;
    public long ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int ProductType { get; set; }
    public int SalesCount { get; set; }
    public decimal SalesAmount { get; set; }
}
