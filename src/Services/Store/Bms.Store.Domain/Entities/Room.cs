namespace Bms.Store.Domain.Entities;

/// <summary>
/// 房间/床位资源
/// 用于预约资源管理，与预约关联
/// </summary>
public class Room : StoreEntity
{
    /// <summary>
    /// 房间/床位名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 编码
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 类型（1:房间 2:床位）
    /// </summary>
    public int RoomType { get; set; }

    /// <summary>
    /// 状态（0:禁用 1:启用）
    /// </summary>
    public int Status { get; set; } = 1;

    /// <summary>
    /// 位置描述
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}
