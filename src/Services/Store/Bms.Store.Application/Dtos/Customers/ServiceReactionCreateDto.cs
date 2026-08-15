namespace Bms.Store.Application.Dtos.Customers;

public class ServiceReactionCreateDto
{
    public long CustomerId { get; set; }
    public long? OrderId { get; set; }
    /// <summary>
    /// 服务项目商品ID（选填，仅服务类商品）
    /// 传值时后端会校验归属并自动填充 ServiceItem 名称快照
    /// </summary>
    public long? ProductId { get; set; }
    public string? ServiceItem { get; set; }
    public DateTime ReactionDate { get; set; }
    public string? Reaction { get; set; }
    public int? Severity { get; set; }
    public string? Remark { get; set; }
}
