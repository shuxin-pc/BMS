namespace Bms.Store.Application.Dtos.Activities;

/// <summary>
/// 更新活动请求 DTO
/// </summary>
public class ActivityUpdateDto : ActivityCreateDto
{
    /// <summary>
    /// 活动ID
    /// </summary>
    public long Id { get; set; }
}
