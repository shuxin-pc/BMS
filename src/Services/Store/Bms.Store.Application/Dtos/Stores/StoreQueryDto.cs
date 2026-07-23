namespace Bms.Store.Application.Dtos.Stores;

/// <summary>
/// 门店分页查询 DTO
/// </summary>
public class StoreQueryDto : PagedRequestDto
{
    /// <summary>
    /// 门店名称（模糊匹配）
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// 门店编码（模糊匹配）
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    /// 状态筛选：1-营业，2-歇业
    /// </summary>
    public int? Status { get; set; }
}
