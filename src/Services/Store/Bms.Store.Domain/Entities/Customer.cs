namespace Bms.Store.Domain.Entities;

/// <summary>
/// 客户档案
/// </summary>
public class Customer : StoreEntity
{
    /// <summary>
    /// 客户姓名
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 手机号
    /// </summary>
    public string Phone { get; set; } = string.Empty;

    /// <summary>
    /// 性别（0:未知 1:男 2:女）
    /// </summary>
    public int Gender { get; set; }

    /// <summary>
    /// 生日
    /// </summary>
    public DateTime? Birthday { get; set; }

    /// <summary>
    /// 客户等级ID
    /// </summary>
    public long? LevelId { get; set; }

    /// <summary>
    /// 累计积分
    /// </summary>
    public int TotalPoints { get; set; }

    /// <summary>
    /// 当前余额
    /// </summary>
    public decimal Balance { get; set; }

    /// <summary>
    /// 累计消费金额
    /// </summary>
    public decimal TotalConsume { get; set; }

    /// <summary>
    /// 最后消费时间
    /// </summary>
    public DateTime? LastConsumeTime { get; set; }

    /// <summary>
    /// 地址
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// 客户标签（JSON格式或逗号分隔）
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// 授权状态（0:未授权 1:已授权 2:已撤回）
    /// 满足《个人信息保护法》合规要求
    /// </summary>
    public int AuthorizationStatus { get; set; }

    /// <summary>
    /// 授权时间
    /// </summary>
    public DateTime? AuthorizationTime { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 导航属性：等级
    /// </summary>
    public CustomerLevel? Level { get; set; }
}
