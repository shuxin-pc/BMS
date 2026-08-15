using Bms.Store.Application.Dtos;

namespace Bms.Store.Application.Dtos.SampleGifts;

/// <summary>
/// 样品统计报表查询参数（按商品维度）
/// </summary>
public class SampleReportQueryDto : PagedRequestDto
{
    /// <summary>
    /// 名称（模糊匹配）
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// 类型（4:样品 5:赠品）
    /// </summary>
    public int? Type { get; set; }

    /// <summary>
    /// 开始日期
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// 结束日期
    /// </summary>
    public DateTime? EndDate { get; set; }
}

/// <summary>
/// 样品/赠品按活动维度统计报表查询参数（P-SG-04）
/// R5：数据源从 SampleGiftOut 迁移到 InventoryLogs（SourceType 9=样品领用出库/10=赠品活动出库）
/// </summary>
public class SampleActivityReportQueryDto : PagedRequestDto
{
    /// <summary>
    /// 活动ID（不传则统计所有活动，含未关联活动的出库）
    /// </summary>
    public long? ActivityId { get; set; }

    /// <summary>
    /// 商品ID（按特定商品筛选）
    /// </summary>
    public long? ProductId { get; set; }

    /// <summary>
    /// 开始日期（按 InventoryLog.CreatedTime 过滤）
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// 结束日期（按 InventoryLog.CreatedTime 过滤）
    /// </summary>
    public DateTime? EndDate { get; set; }
}

/// <summary>
/// 样品/赠品按客户维度统计报表查询参数（P-SG-04）
/// 数据源：SampleGiftReceive（样品领用）
/// </summary>
public class SampleCustomerReportQueryDto : PagedRequestDto
{
    /// <summary>
    /// 客户ID（按特定客户筛选）
    /// </summary>
    public long? CustomerId { get; set; }

    /// <summary>
    /// 客户名称（模糊匹配）
    /// </summary>
    public string? CustomerName { get; set; }

    /// <summary>
    /// 开始日期（按 ReceiveTime 过滤）
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// 结束日期（按 ReceiveTime 过滤）
    /// </summary>
    public DateTime? EndDate { get; set; }
}
