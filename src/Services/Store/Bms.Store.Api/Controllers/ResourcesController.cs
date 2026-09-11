using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bms.BuildingBlocks.Abstractions.Security;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Resources;
using Bms.Store.Application.Services.Resources;

namespace Bms.Store.Api.Controllers;

/// <summary>
/// 资源冲突检测与可用性查询控制器
/// 用于技师/房间/设备的占用查询（前端列表标红、提交校验）
/// </summary>
[ApiController]
[Route("api/store/[controller]")]
[Authorize]
public class ResourcesController : ControllerBase
{
    private readonly IResourceAvailabilityService _availabilityService;
    private readonly IResourceConflictCheckService _conflictCheckService;
    private readonly ICurrentUser _currentUser;

    public ResourcesController(
        IResourceAvailabilityService availabilityService,
        IResourceConflictCheckService conflictCheckService,
        ICurrentUser currentUser)
    {
        _availabilityService = availabilityService;
        _conflictCheckService = conflictCheckService;
        _currentUser = currentUser;
    }

    /// <summary>
    /// 查询指定时段内技师/房间/设备的可用性（列表标红用）
    /// 返回全量启用资源，每项带 IsOccupied 标记供前端标红
    /// </summary>
    /// <param name="startTime">占用开始时间（含）</param>
    /// <param name="endTime">占用结束时间（不含）</param>
    /// <param name="storeId">门店ID（可空，null=当前租户全部门店）</param>
    /// <param name="roomType">房间类型过滤（1:房间 2:床位，null=不过滤）；serviceProductId 优先级更高</param>
    /// <param name="serviceProductId">服务项目ID（可空，传入后按其 RequiredRoomType 自动过滤房间）</param>
    /// <param name="excludeAppointmentId">需排除占用的预约ID（可空；预约转订单行编辑时排除其源预约，避免自身来源占用标红/拦截）</param>
    [HttpGet("availability")]
    public async Task<ApiResponseDto<ResourceAvailabilityDto>> GetAvailability(
        [FromQuery] DateTime startTime,
        [FromQuery] DateTime endTime,
        [FromQuery] long? storeId = null,
        [FromQuery] int? roomType = null,
        [FromQuery] long? serviceProductId = null,
        [FromQuery] long? excludeAppointmentId = null)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<ResourceAvailabilityDto>.Fail("登录状态异常，请重新登录", 401);

        // storeId 缺省时取当前用户的门店
        var effectiveStoreId = storeId ?? _currentUser.StoreId;
        var result = await _availabilityService.GetAvailabilityAsync(
            _currentUser.TenantId.Value, effectiveStoreId, startTime, endTime, roomType, serviceProductId, excludeAppointmentId);
        return ApiResponseDto<ResourceAvailabilityDto>.Ok(result);
    }
}
