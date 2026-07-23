namespace Bms.Store.Application.Dtos.Stores;

/// <summary>
/// 更新门店请求 DTO
/// </summary>
public class StoreUpdateDto : StoreCreateDto
{
    /// <summary>
    /// 门店ID
    /// </summary>
    public long Id { get; set; }
}
