namespace Bms.Store.Application.Dtos.Rooms;

/// <summary>
/// 房间床位输出 DTO
/// </summary>
public class RoomDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int RoomType { get; set; }
    public int Status { get; set; }
    public string? Location { get; set; }
    public string? Remark { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
