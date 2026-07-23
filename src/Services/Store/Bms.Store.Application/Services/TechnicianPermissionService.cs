using Bms.Store.Domain.Entities;

namespace Bms.Store.Application.Services;

/// <summary>
/// 技师权限服务实现
/// 平台租户ID固定为 1，平台技师归属于平台租户
/// </summary>
public class TechnicianPermissionService : ITechnicianPermissionService
{
    /// <summary>
    /// 平台租户ID（平台技师归属于此租户，商家门店可只读选择平台技师）
    /// </summary>
    private const long PlatformTenantId = 1;

    /// <inheritdoc />
    public bool IsPlatformTenant(long tenantId) => tenantId == PlatformTenantId;

    /// <inheritdoc />
    public bool CanCreatePlatformTechnician(long currentTenantId) => IsPlatformTenant(currentTenantId);

    /// <inheritdoc />
    public bool CanModifyTechnician(long currentTenantId, Technician technician)
        => technician.TenantId == currentTenantId;
}
