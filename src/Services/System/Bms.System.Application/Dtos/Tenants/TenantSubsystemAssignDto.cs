namespace Bms.System.Application.Dtos.Tenants;

/// <summary>
/// 租户子系统分配 DTO
/// </summary>
public class TenantSubsystemAssignDto
{
    /// <summary>
    /// 子系统ID列表
    /// </summary>
    public List<long> SubsystemIds { get; set; } = new List<long>();
}
