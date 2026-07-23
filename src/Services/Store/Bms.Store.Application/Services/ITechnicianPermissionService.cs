using Bms.Store.Domain.Entities;

namespace Bms.Store.Application.Services;

/// <summary>
/// 技师权限服务接口
/// 封装平台租户判定与平台技师操作权限校验逻辑，便于复用与测试 Mock
/// </summary>
public interface ITechnicianPermissionService
{
    /// <summary>
    /// 判定指定租户是否为平台租户
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <returns>是平台租户返回 true，否则 false</returns>
    bool IsPlatformTenant(long tenantId);

    /// <summary>
    /// 判定当前租户是否有权创建平台技师
    /// 仅平台租户可创建平台技师（Source=2），商家租户创建的技师均为自有技师（Source=1）
    /// </summary>
    /// <param name="currentTenantId">当前租户ID</param>
    /// <returns>有权创建平台技师返回 true，否则 false</returns>
    bool CanCreatePlatformTechnician(long currentTenantId);

    /// <summary>
    /// 判定当前租户是否有权修改指定技师
    /// 商家租户仅可修改本租户技师；平台租户可修改平台技师
    /// </summary>
    /// <param name="currentTenantId">当前租户ID</param>
    /// <param name="technician">待修改的技师实体</param>
    /// <returns>有权修改返回 true，否则 false</returns>
    bool CanModifyTechnician(long currentTenantId, Technician technician);
}
