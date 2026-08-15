using Bms.Store.Application.Dtos;

namespace Bms.Store.Application.Dtos.Orders;

/// <summary>
/// 订单分页查询参数
/// </summary>
public class OrderQueryDto : PagedRequestDto
{
    /// <summary>
    /// 订单编号（模糊匹配）
    /// </summary>
    public string? OrderNo { get; set; }

    /// <summary>
    /// 客户ID
    /// </summary>
    public long? CustomerId { get; set; }

    /// <summary>
    /// 客户姓名（模糊匹配，散客订单不匹配）
    /// </summary>
    public string? CustomerName { get; set; }

    /// <summary>
    /// 客户手机号（模糊匹配）
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// 订单类型（1:零售 2:服务 3:疗程卡核销 4:储值消费）
    /// </summary>
    public int? OrderType { get; set; }

    /// <summary>
    /// 订单状态（1:进行中 2:已完成 3:已退款 4:已取消）
    /// </summary>
    public int? Status { get; set; }

    /// <summary>
    /// 支付方式（1:现金 2:支付宝 3:微信 4:银行卡 5:储值卡）
    /// </summary>
    public int? PayMethod { get; set; }

    /// <summary>
    /// 补录状态（0:非补录 1:待补录）
    /// </summary>
    public int? BackfillStatus { get; set; }

    /// <summary>
    /// 下单开始日期（含，按 OrderTime 过滤）
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// 下单结束日期（含当天，查询时自动加一天）
    /// </summary>
    public DateTime? EndDate { get; set; }
}
