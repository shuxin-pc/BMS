using Microsoft.AspNetCore.Mvc;

namespace Bms.BuildingBlocks.Core.HealthChecks;

/// <summary>
/// 健康检查控制器
/// </summary>
[ApiController]
[Route("[controller]")]
public class HealthCheckController : ControllerBase
{
    /// <summary>
    /// 健康检查接口
    /// </summary>
    /// <returns>健康状态信息</returns>
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.Now });
    }
}
