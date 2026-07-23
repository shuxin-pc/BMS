namespace Bms.Store.Application.Dtos.Customers;

/// <summary>
/// 客户消费统计DTO
/// 用于前端客户详情页展示消费频次、客单价、消费偏好
/// 偏好维度：按订单类型、商品类型、商品分类、时段、技师分组
/// 仅统计近 6 个月数据，避免历史数据干扰
/// </summary>
public class CustomerConsumptionStatDto
{
    /// <summary>
    /// 客户ID
    /// </summary>
    public long CustomerId { get; set; }

    /// <summary>
    /// 消费频次（已完成订单数，不含退款/取消）
    /// </summary>
    public int OrderCount { get; set; }

    /// <summary>
    /// 累计消费金额（实付金额合计）
    /// </summary>
    public decimal TotalConsumption { get; set; }

    /// <summary>
    /// 客单价（累计消费/订单数，0=无订单）
    /// </summary>
    public decimal AverageOrderValue { get; set; }

    /// <summary>
    /// 最近消费时间
    /// </summary>
    public DateTime? LastConsumeTime { get; set; }

    /// <summary>
    /// 消费偏好（按订单类型分组的金额占比）
    /// 保留字段，兼容前端现有契约
    /// </summary>
    public List<ConsumptionPreferenceItem> Preferences { get; set; } = new();

    /// <summary>
    /// 按商品类型分组的消费偏好（1:实物商品 2:服务商品 3:耗材 4:样品 5:赠品）
    /// </summary>
    public List<ConsumePreferenceItem> PreferencesByProductType { get; set; } = new();

    /// <summary>
    /// 按商品分类分组的消费偏好
    /// </summary>
    public List<ConsumePreferenceItem> PreferencesByCategory { get; set; } = new();

    /// <summary>
    /// 按时段分组的消费偏好（上午/下午/晚上/凌晨）
    /// </summary>
    public List<ConsumePreferenceItem> PreferencesByTimeSlot { get; set; } = new();

    /// <summary>
    /// 按技师分组的消费偏好
    /// </summary>
    public List<ConsumePreferenceItem> PreferencesByTechnician { get; set; } = new();
}

/// <summary>
/// 消费偏好项（按订单类型分组的旧版结构，保留兼容）
/// </summary>
public class ConsumptionPreferenceItem
{
    /// <summary>
    /// 商品类型（1:实物 2:服务 3:耗材 4:疗程卡）
    /// </summary>
    public int ProductType { get; set; }

    /// <summary>
    /// 商品类型名称
    /// </summary>
    public string ProductTypeName { get; set; } = string.Empty;

    /// <summary>
    /// 该类型消费金额
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// 金额占比（0-1）
    /// </summary>
    public decimal Percentage { get; set; }
}

/// <summary>
/// 通用消费偏好项（按任意维度分组）
/// 用于商品类型、商品分类、时段、技师等维度的偏好统计
/// </summary>
public class ConsumePreferenceItem
{
    /// <summary>
    /// 维度键名称（如商品类型名、分类名、时段名、技师名）
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// 订单数
    /// </summary>
    public int OrderCount { get; set; }

    /// <summary>
    /// 消费金额合计
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// 订单数占比（0-1，按订单数计算）
    /// </summary>
    public decimal Percentage { get; set; }
}
