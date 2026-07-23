using Bms.Store.Application.Dtos;

namespace Bms.Store.Application.Dtos.StoredValues;

/// <summary>
/// 储值规则分页查询 DTO
/// </summary>
public class StoredValueRuleQueryDto : PagedRequestDto
{
    /// <summary>
    /// 规则名称（模糊搜索）
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool? IsEnabled { get; set; }
}
