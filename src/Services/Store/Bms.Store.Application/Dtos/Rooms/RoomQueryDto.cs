using Bms.Store.Application.Dtos;

namespace Bms.Store.Application.Dtos.Rooms;

/// <summary>
/// 房间床位分页查询参数
/// </summary>
public class RoomQueryDto : PagedRequestDto
{
    public string? Name { get; set; }
    public string? Code { get; set; }
    public int? RoomType { get; set; }
    public int? Status { get; set; }
}
