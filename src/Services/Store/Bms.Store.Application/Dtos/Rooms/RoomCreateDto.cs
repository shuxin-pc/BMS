namespace Bms.Store.Application.Dtos.Rooms;

/// <summary>
/// 创建房间床位输入 DTO
/// </summary>
public class RoomCreateDto
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int RoomType { get; set; }
    public int Status { get; set; } = 1;
    public string? Location { get; set; }
    public string? Remark { get; set; }
}
