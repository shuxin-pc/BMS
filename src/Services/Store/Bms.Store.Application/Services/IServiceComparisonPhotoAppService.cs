using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Customers;

namespace Bms.Store.Application.Services;

public interface IServiceComparisonPhotoAppService
{
    Task<ApiResponseDto<PagedResponseDto<ServiceComparisonPhotoDto>>> GetPagedListAsync(ServiceComparisonPhotoQueryDto query);
    Task<ApiResponseDto<ServiceComparisonPhotoDto?>> GetByIdAsync(long id);
    Task<ApiResponseDto<ServiceComparisonPhotoDto>> CreateAsync(ServiceComparisonPhotoCreateDto dto);
    Task<ApiResponseDto<ServiceComparisonPhotoDto>> UpdateAsync(ServiceComparisonPhotoUpdateDto dto);
    Task<ApiResponseDto> DeleteAsync(long id);
    Task<ApiResponseDto> BatchDeleteAsync(List<long> ids);
}
