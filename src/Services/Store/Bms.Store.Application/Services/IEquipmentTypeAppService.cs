using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.EquipmentTypes;

namespace Bms.Store.Application.Services;

/// <summary>
/// 设备类型应用服务接口
/// </summary>
public interface IEquipmentTypeAppService
{
    Task<ApiResponseDto<PagedResponseDto<EquipmentTypeDto>>> GetPagedListAsync(EquipmentTypeQueryDto query);

    /// <summary>
    /// 获取全部启用设备类型（用于下拉选择）
    /// </summary>
    Task<ApiResponseDto<List<EquipmentTypeDto>>> GetAllAsync();

    Task<ApiResponseDto<EquipmentTypeDto?>> GetByIdAsync(long id);
    Task<ApiResponseDto<EquipmentTypeDto>> CreateAsync(EquipmentTypeCreateDto dto);
    Task<ApiResponseDto<EquipmentTypeDto>> UpdateAsync(EquipmentTypeUpdateDto dto);
    Task<ApiResponseDto> DeleteAsync(long id);
}
