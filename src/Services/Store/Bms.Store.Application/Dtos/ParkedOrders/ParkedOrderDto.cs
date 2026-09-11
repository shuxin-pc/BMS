namespace Bms.Store.Application.Dtos.ParkedOrders;

/// <summary>
/// 挂单 DTO
/// 列表接口不返回 CartJson（仅返回统计字段），取单接口返回完整 CartJson 供前端恢复
/// </summary>
public class ParkedOrderDto
{
    /// <summary>
    /// 挂单ID
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 挂单号（PK{yyyyMMdd}{序号}）
    /// </summary>
    public string ParkNo { get; set; } = string.Empty;

    /// <summary>
    /// 会员客户ID（散客为空）
    /// </summary>
    public long? CustomerId { get; set; }

    /// <summary>
    /// 会员客户姓名（冗余展示）
    /// </summary>
    public string? CustomerName { get; set; }

    /// <summary>
    /// 挂单备注
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 挂单人姓名（冗余展示，取单列表识别谁挂的单）
    /// </summary>
    public string? CreatedByName { get; set; }

    /// <summary>
    /// 状态（1=挂起 2=已取走 3=已取消）
    /// </summary>
    public int Status { get; set; }

    /// <summary>
    /// 挂单时间
    /// </summary>
    public DateTime HeldTime { get; set; }

    /// <summary>
    /// 购物车行数（后端解析 CartJson 计算，列表展示）
    /// </summary>
    public int ItemCount { get; set; }

    /// <summary>
    /// 购物车商品合计金额（仅商品/服务行 price×quantity，列表展示）
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// 购物车 JSON 快照（仅取单接口返回，前端 JSON.parse 恢复购物车）
    /// </summary>
    public string? CartJson { get; set; }
}
