using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Equipments;

namespace Bms.Store.Application.Services;

/// <summary>
/// 设备台账应用服务接口
/// </summary>
public interface IEquipmentAppService
{
    Task<ApiResponseDto<PagedResponseDto<EquipmentDto>>> GetPagedListAsync(EquipmentQueryDto query);
    Task<ApiResponseDto<EquipmentDto?>> GetByIdAsync(long id);
    Task<ApiResponseDto<EquipmentDto>> CreateAsync(EquipmentCreateDto dto);
    Task<ApiResponseDto<EquipmentDto>> UpdateAsync(EquipmentUpdateDto dto);
    Task<ApiResponseDto> DeleteAsync(long id);
    Task<ApiResponseDto> BatchDeleteAsync(List<long> ids);

    /// <summary>
    /// 查询即将到期保养的设备列表（NextMaintenanceDate 在未来 days 天内，含已过期未保养）
    /// </summary>
    /// <param name="days">未来天数（默认 7）</param>
    /// <returns>按 NextMaintenanceDate 升序排列的设备列表</returns>
    Task<ApiResponseDto<List<EquipmentDto>>> GetUpcomingMaintenanceAsync(int days = 7);

    /// <summary>
    /// 根据服务项目查询可用设备列表
    /// 按服务项目 ServiceProductEquipment 关联表查询允许的设备类型，并排除指定时段已冲突的设备
    /// </summary>
    /// <param name="serviceProductId">服务项目子表ID（关联 ServiceProduct.Id）</param>
    /// <param name="startTime">预约开始时间</param>
    /// <param name="endTime">预约结束时间</param>
    /// <param name="excludeAppointmentId">需排除的预约ID（更新场景，避免与自身冲突）</param>
    /// <returns>可用设备列表；服务项目未关联设备类型时返回空列表</returns>
    Task<ApiResponseDto<List<EquipmentDto>>> GetAvailableByServiceProductAsync(
        long serviceProductId,
        DateTime startTime,
        DateTime endTime,
        long? excludeAppointmentId = null);
}
