namespace Bms.Store.Application.Dtos.Customers;

public class ServiceComparisonPhotoCreateDto
{
    public long CustomerId { get; set; }
    public long? OrderId { get; set; }
    /// <summary>
    /// 服务项目商品ID（选填，仅服务类商品）
    /// 传值时后端会校验归属并自动填充 ServiceItem 名称快照
    /// </summary>
    public long? ProductId { get; set; }
    public string? ServiceItem { get; set; }
    public DateTime PhotoDate { get; set; }
    public int PhotoType { get; set; }
    /// <summary>
    /// 照片明细（至少一张，按数组顺序决定展示排序）
    /// </summary>
    public List<ServiceComparisonPhotoItemSaveDto> Items { get; set; } = new();
    public string? Remark { get; set; }
}
