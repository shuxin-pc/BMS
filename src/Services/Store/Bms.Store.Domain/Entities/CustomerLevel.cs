namespace Bms.Store.Domain.Entities;

/// <summary>
/// 客户等级
/// 系统仅允许创建普通会员(Level=1)和会员(Level=2)两个等级
/// 依据：G5.2 客户等级仅支持普通/会员两级
/// </summary>
public class CustomerLevel : StoreEntity
{
    /// <summary>
    /// 等级名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 等级编码
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 等级值（1:普通会员 2:会员），创建后不可修改
    /// 参见 <see cref="Constants.CustomerLevelTypes"/>
    /// </summary>
    public int Level { get; set; }

    /// <summary>
    /// 折扣率
    /// </summary>
    public decimal DiscountRate { get; set; } = 1.0m;

    /// <summary>
    /// 排序
    /// </summary>
    public int Sort { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}
