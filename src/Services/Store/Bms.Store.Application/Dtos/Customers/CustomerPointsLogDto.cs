namespace Bms.Store.Application.Dtos.Customers;

/// <summary>
/// 客户积分流水输出 DTO
/// </summary>
public class CustomerPointsLogDto
{
    public long Id { get; set; }
    public long CustomerId { get; set; }

    /// <summary>
    /// 客户姓名（关联 Customer 表查询）
    /// </summary>
    public string? CustomerName { get; set; }

    /// <summary>
    /// 手机号（关联 Customer 表查询）
    /// </summary>
    public string? Phone { get; set; }

    public int Type { get; set; }
    public int Points { get; set; }
    public int BeforePoints { get; set; }
    public int AfterPoints { get; set; }
    public long? OrderId { get; set; }

    /// <summary>
    /// 关联订单号（关联 Order 表查询，无订单时为 null）
    /// </summary>
    public string? OrderNo { get; set; }

    public long? OperatorId { get; set; }
    public DateTime? ExpireDate { get; set; }
    public string? Remark { get; set; }

    /// <summary>
    /// 变动时间（对应实体 CreatedTime，前端展示用）
    /// </summary>
    public DateTime ChangeTime { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
