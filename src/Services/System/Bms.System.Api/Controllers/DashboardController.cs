using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bms.BuildingBlocks.Abstractions.Security;
using Bms.System.Application.Dtos;
using Bms.System.Application.Dtos.Dashboard;
using Bms.System.Domain.IRepositories;

namespace Bms.System.Api.Controllers;

/// <summary>
/// 系统首页统计控制器
/// </summary>
[ApiController]
[Route("api/system/[controller]")]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ICurrentUser _currentUser;

    public DashboardController(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        ITenantRepository tenantRepository,
        IAuditLogRepository auditLogRepository,
        ICurrentUser currentUser)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _tenantRepository = tenantRepository;
        _auditLogRepository = auditLogRepository;
        _currentUser = currentUser;
    }

    /// <summary>
    /// 获取系统首页统计数据
    /// 数据隔离口径与用户管理页一致：非超级管理员仅统计本租户；
    /// 租户总数仅平台租户（超级管理员）返回，其余租户为 null
    /// </summary>
    /// <returns>首页统计数据</returns>
    [HttpGet("stats")]
    public async Task<ApiResponseDto<DashboardStatsDto>> GetStats()
    {
        // 非平台租户强制限定本租户；超级管理员（平台租户）统计全局
        var tenantId = _currentUser.IsSuperAdmin ? null : _currentUser.TenantId;

        var dto = new DashboardStatsDto
        {
            UserCount = await _userRepository.GetCountAsync(tenantId: tenantId),
            RoleCount = await _roleRepository.GetCountAsync(tenantId),
            TodayAuditCount = await _auditLogRepository.GetCountAsync(startDate: DateTime.Today, tenantId: tenantId),
            IsPlatformTenant = _currentUser.IsSuperAdmin
        };

        if (_currentUser.IsSuperAdmin)
        {
            dto.TenantCount = await _tenantRepository.GetCountAsync();
        }

        return ApiResponseDto<DashboardStatsDto>.Success(dto);
    }
}
