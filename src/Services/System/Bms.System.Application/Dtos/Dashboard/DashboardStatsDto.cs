namespace Bms.System.Application.Dtos.Dashboard;

/// <summary>
/// 系统首页统计数据
/// </summary>
public class DashboardStatsDto
{
    /// <summary>
    /// 用户总数（非平台租户仅统计本租户）
    /// </summary>
    public int UserCount { get; set; }

    /// <summary>
    /// 角色总数（非平台租户仅统计本租户）
    /// </summary>
    public int RoleCount { get; set; }

    /// <summary>
    /// 租户总数（仅平台租户返回，其余租户为 null）
    /// </summary>
    public int? TenantCount { get; set; }

    /// <summary>
    /// 今日审计操作数（非平台租户仅统计本租户）
    /// </summary>
    public int TodayAuditCount { get; set; }

    /// <summary>
    /// 当前登录用户是否平台租户（超级管理员），控制前端租户模块显隐
    /// </summary>
    public bool IsPlatformTenant { get; set; }
}
