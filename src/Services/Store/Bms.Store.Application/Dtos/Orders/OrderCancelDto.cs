namespace Bms.Store.Application.Dtos.Orders;

/// <summary>
/// 取消订单请求 DTO
/// 商户在订单列表页点击"取消订单"时提交，后端执行完整事务回滚（库存/BOM/疗程卡次数等），订单 Status 改为 4（已取消）
/// </summary>
public class OrderCancelDto
{
    /// <summary>
    /// 取消原因（必填，用于审计追溯）
    /// </summary>
    public string Reason { get; set; } = string.Empty;
}
