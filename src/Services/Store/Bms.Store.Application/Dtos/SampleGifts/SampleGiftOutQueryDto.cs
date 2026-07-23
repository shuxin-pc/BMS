using Bms.Store.Application.Dtos;

namespace Bms.Store.Application.Dtos.SampleGifts;

/// <summary>
/// 赠品出库记录分页查询参数
/// </summary>
public class SampleGiftOutQueryDto : PagedRequestDto
{
    public long? ProductId { get; set; }
}
