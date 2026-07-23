using Bms.Store.Application.Dtos;

namespace Bms.Store.Application.Dtos.SampleGifts;

/// <summary>
/// 样品库存查询参数
/// </summary>
public class SampleInventoryQueryDto : PagedRequestDto
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
    /// 库存状态（1:充足 2:偏低 3:不足）
    /// </summary>
    public int? InventoryStatus { get; set; }
}
