using Bms.Store.Application.Dtos;

namespace Bms.Store.Application.Dtos.StoredValues;

/// <summary>
/// 储值账户分页查询 DTO
/// </summary>
public class StoredValueAccountQueryDto : PagedRequestDto
{
    /// <summary>
    /// 客户ID
    /// </summary>
    public long? CustomerId { get; set; }
}
