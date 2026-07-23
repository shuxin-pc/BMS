using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Equipments;

namespace Bms.Store.Application.Services;

/// <summary>
/// 设备维护记录应用服务接口
/// </summary>
public interface IEquipmentMaintenanceAppService
{
    Task<ApiResponseDto<PagedResponseDto<EquipmentMaintenanceDto>>> GetPagedListAsync(EquipmentMaintenanceQueryDto query);
    Task<ApiResponseDto<EquipmentMaintenanceDto?>> GetByIdAsync(long id);
    Task<ApiResponseDto<EquipmentMaintenanceDto>> CreateAsync(EquipmentMaintenanceCreateDto dto);
    Task<ApiResponseDto<EquipmentMaintenanceDto>> UpdateAsync(EquipmentMaintenanceUpdateDto dto);
    Task<ApiResponseDto> DeleteAsync(long id);
    Task<ApiResponseDto> BatchDeleteAsync(List<long> ids);
}
