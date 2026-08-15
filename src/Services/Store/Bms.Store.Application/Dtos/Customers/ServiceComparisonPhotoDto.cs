namespace Bms.Store.Application.Dtos.Customers;

public class ServiceComparisonPhotoDto
{
    public long Id { get; set; }
    public long CustomerId { get; set; }
    /// <summary>
    /// 客户名称（展示用，由查询时 join Customer 表填充）
    /// </summary>
    public string? CustomerName { get; set; }
    /// <summary>
    /// 客户手机号（展示用，由查询时 join Customer 表填充）
    /// </summary>
    public string? CustomerPhone { get; set; }
    public long? OrderId { get; set; }
    /// <summary>
    /// 关联订单号（展示用，由查询时单独查询 Orders 表填充）
    /// </summary>
    public string? OrderNo { get; set; }
    /// <summary>
    /// 服务项目商品ID
    /// </summary>
    public long? ProductId { get; set; }
    /// <summary>
    /// 服务项目商品当前名称（展示用，关联 ProductMaster 表）
    /// 与 ServiceItem 快照的区别：此字段随商品改名变化，快照不变
    /// </summary>
    public string? ProductName { get; set; }
    public string? ServiceItem { get; set; }
    public DateTime PhotoDate { get; set; }
    public int PhotoType { get; set; }
    /// <summary>
    /// 照片明细（按 SortOrder 升序，同一次服务的同一类型可有多张）
    /// </summary>
    public List<ServiceComparisonPhotoItemDto> Items { get; set; } = new();
    public string? Remark { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
