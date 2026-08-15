using Bms.Store.Application.Dtos;

namespace Bms.Store.Application.Dtos.Inventories;

/// <summary>
/// 库存盘点记录分页查询参数
/// </summary>
public class InventoryCheckQueryDto : PagedRequestDto
{
    public long? ProductId { get; set; }

    /// <summary>
    /// 盘点单状态（0=草稿 1=已完成 2=已取消）
    /// </summary>
    public int? Status { get; set; }

    /// <summary>
    /// 商品名称（模糊匹配，联表 Product 查询）
    /// </summary>
    public string? ProductName { get; set; }

    /// <summary>
    /// 盘点时间起始日期（按 CheckTime 筛选）
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// 盘点时间结束日期（按 CheckTime 筛选）
    /// </summary>
    public DateTime? EndDate { get; set; }
}
