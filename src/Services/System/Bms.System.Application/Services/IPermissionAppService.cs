using Bms.System.Application.Dtos;
using Bms.System.Application.Dtos.Permissions;

namespace Bms.System.Application.Services;

public interface IPermissionAppService
{
    Task<PagedResponseDto<PermissionDto>> GetPagedListAsync(PagedRequestDto request);
    Task<List<PermissionDto>> GetListByMenuIdAsync(long menuId);
    Task<PermissionDto?> GetByIdAsync(long id);
    Task<PermissionDto> CreateAsync(PermissionCreateDto dto);
    Task<PermissionDto> UpdateAsync(PermissionUpdateDto dto);
    Task DeleteAsync(long id);
}
