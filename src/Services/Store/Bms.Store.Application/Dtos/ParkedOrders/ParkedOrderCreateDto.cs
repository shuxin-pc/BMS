namespace Bms.Store.Application.Dtos.ParkedOrders;

/// <summary>
/// 挂单创建 DTO
/// CartJson 为购物车数组序列化字符串（前端 JSON.stringify(cart)），
/// 后端仅校验其为合法 JSON 数组后原样存取，取单时原样返回供前端恢复
/// </summary>
public class ParkedOrderCreateDto
{
    /// <summary>
    /// 会员客户ID（散客为空）
    /// </summary>
    public long? CustomerId { get; set; }

    /// <summary>
    /// 会员客户姓名（冗余，列表展示用）
    /// </summary>
    public string? CustomerName { get; set; }

    /// <summary>
    /// 挂单备注（客户称呼/电话等，选填，便于取单识别）
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 购物车 JSON 快照（非空 JSON 数组）
    /// </summary>
    public string CartJson { get; set; } = string.Empty;
}
