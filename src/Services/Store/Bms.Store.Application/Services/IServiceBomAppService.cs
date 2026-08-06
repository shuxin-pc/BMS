using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.ServiceBoms;

namespace Bms.Store.Application.Services;

/// <summary>
/// 服务BOM应用服务接口
/// </summary>
public interface IServiceBomAppService
{
    Task<ApiResponseDto<PagedResponseDto<ServiceBomDto>>> GetPagedListAsync(ServiceBomQueryDto query);
    Task<ApiResponseDto<ServiceBomDto?>> GetByIdAsync(long id);
    Task<ApiResponseDto<ServiceBomDto>> CreateAsync(ServiceBomCreateDto dto);
    Task<ApiResponseDto<ServiceBomDto>> UpdateAsync(ServiceBomUpdateDto dto);
    Task<ApiResponseDto> DeleteAsync(long id);
    Task<ApiResponseDto> BatchDeleteAsync(List<long> ids);

    /// <summary>
    /// 获取服务项目选项列表（用于BOM下拉选择）
    /// </summary>
    Task<ApiResponseDto<List<ServiceProductOptionDto>>> GetServiceProductOptionsAsync();

    /// <summary>
    /// 获取耗材商品选项列表（用于BOM下拉选择，仅 type=3 耗材）
    /// </summary>
    Task<ApiResponseDto<List<ConsumableOptionDto>>> GetConsumableOptionsAsync();
}
