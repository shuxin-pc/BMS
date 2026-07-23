using Bms.Store.Application.Dtos;

namespace Bms.Store.Application.Dtos.SampleGifts;

/// <summary>
/// 样品领用记录分页查询参数
/// </summary>
public class SampleGiftReceiveQueryDto : PagedRequestDto
{
    public long? ProductId { get; set; }
    public long? CustomerId { get; set; }
}
