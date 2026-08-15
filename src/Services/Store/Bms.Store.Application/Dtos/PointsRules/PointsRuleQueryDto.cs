using Bms.Store.Application.Dtos;

namespace Bms.Store.Application.Dtos.PointsRules;

/// <summary>
/// 积分规则分页查询DTO
/// </summary>
public class PointsRuleQueryDto : PagedRequestDto
{
    public int? Status { get; set; }
}
