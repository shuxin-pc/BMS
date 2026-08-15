using Bms.Store.Application.Dtos;

namespace Bms.Store.Application.Dtos.StoredValues;

/// <summary>
/// 储值流水分页查询 DTO
/// 查询视角（文档 4.3）：
/// - 门店视角：未指定 CustomerId 时，AppService 按 TenantId + StoreId = 当前门店过滤（门店充值/消费业绩）
/// - 客户视角：指定 CustomerId 时，AppService 按 TenantId + CustomerId 过滤，不按 StoreId 过滤（跨店消费历史需完整可见）
/// - 跨店统计视角：Log.StoreId ≠ Account.StoreId 的消费记录
/// </summary>
public class StoredValueLogQueryDto : PagedRequestDto
{
    /// <summary>
    /// 客户ID
    /// </summary>
    public long? CustomerId { get; set; }

    /// <summary>
    /// 流水类型（1:充值 2:消费 3:退款 4:调整）
    /// </summary>
    public int? Type { get; set; }

    /// <summary>客户姓名（模糊匹配）</summary>
    public string? CustomerName { get; set; }

    /// <summary>客户手机号（模糊匹配）</summary>
    public string? Phone { get; set; }

    /// <summary>开始日期（CreatedTime >= StartDate）</summary>
    public DateTime? StartDate { get; set; }

    /// <summary>结束日期（CreatedTime <= EndDate，含当日）</summary>
    public DateTime? EndDate { get; set; }
}
