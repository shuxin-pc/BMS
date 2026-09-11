namespace Bms.Store.Application.Dtos.ParkedOrders;

/// <summary>
/// 挂单分页查询 DTO
/// 仅查挂起状态（Status=1）的挂单，历史（已取走/已取消）通过状态字段归档不展示
/// </summary>
public class ParkedOrderQueryDto
{
    /// <summary>
    /// 关键字（挂单号/备注/会员姓名 模糊匹配）
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// 页码
    /// </summary>
    public int PageIndex { get; set; } = 1;

    /// <summary>
    /// 每页条数
    /// </summary>
    public int PageSize { get; set; } = 20;
}
