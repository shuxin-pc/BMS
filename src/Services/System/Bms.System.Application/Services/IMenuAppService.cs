using Bms.System.Application.Dtos;
using Bms.System.Application.Dtos.Menus;

namespace Bms.System.Application.Services;

public interface IMenuAppService
{
    Task<ApiResponseDto<List<MenuDto>>> GetTreeListAsync();
    Task<ApiResponseDto<List<MenuDto>>> GetListAsync(MenuQueryDto query);
    Task<ApiResponseDto<List<MenuDto>>> GetUserMenusAsync(long userId);
    Task<ApiResponseDto<List<SubsystemMenusDto>>> GetAuthorizedAllAsync(long userId);
    Task<ApiResponseDto<MenuDto?>> GetByIdAsync(long id);
    Task<ApiResponseDto<MenuDto>> CreateAsync(MenuCreateDto dto);
    Task<ApiResponseDto<MenuDto>> UpdateAsync(MenuUpdateDto dto);
    Task<ApiResponseDto> DeleteAsync(long id);
}
