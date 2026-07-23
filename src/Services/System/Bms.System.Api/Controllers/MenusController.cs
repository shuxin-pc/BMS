using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bms.System.Application.Dtos;
using Bms.System.Application.Dtos.Menus;
using Bms.System.Application.Services;

namespace Bms.System.Api.Controllers;

[ApiController]
[Route("api/system/[controller]")]
[Route("api/[controller]")]
[Authorize]
public class MenusController : ControllerBase
{
    private readonly IMenuAppService _menuService;

    public MenusController(IMenuAppService menuService)
    {
        _menuService = menuService;
    }

    /// <summary>
    /// 获取菜单树形列表
    /// </summary>
    [HttpGet("tree")]
    public async Task<ApiResponseDto<List<MenuDto>>> GetTree()
    {
        return await _menuService.GetTreeListAsync();
    }

    /// <summary>
    /// 获取菜单列表
    /// </summary>
    [HttpGet]
    public async Task<ApiResponseDto<List<MenuDto>>> GetList([FromQuery] MenuQueryDto query)
    {
        return await _menuService.GetListAsync(query);
    }

    /// <summary>
    /// 获取用户可访问的菜单
    /// </summary>
    [HttpGet("user/{userId}")]
    public async Task<ApiResponseDto<List<MenuDto>>> GetUserMenus(long userId)
    {
        return await _menuService.GetUserMenusAsync(userId);
    }

    /// <summary>
    /// 获取菜单详情
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ApiResponseDto<MenuDto?>> GetById(long id)
    {
        return await _menuService.GetByIdAsync(id);
    }

    /// <summary>
    /// 创建菜单（支持内部服务调用）
    /// </summary>
    [HttpPost]
    public async Task<ApiResponseDto<MenuDto>> Create([FromBody] MenuCreateDto dto)
    {
        try
        {
            return await _menuService.CreateAsync(dto);
        }
        catch (InvalidOperationException ex)
        {
            return ApiResponseDto<MenuDto>.Fail(ex.Message, 400);
        }
    }

    /// <summary>
    /// 更新菜单
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ApiResponseDto<MenuDto>> Update(long id, [FromBody] MenuUpdateDto dto)
    {
        try
        {
            dto.Id = id;
            return await _menuService.UpdateAsync(dto);
        }
        catch (InvalidOperationException ex)
        {
            return ApiResponseDto<MenuDto>.Fail(ex.Message, 400);
        }
    }

    /// <summary>
    /// 删除菜单
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ApiResponseDto> Delete(long id)
    {
        try
        {
            return await _menuService.DeleteAsync(id);
        }
        catch (InvalidOperationException ex)
        {
            return ApiResponseDto.Fail(ex.Message, 400);
        }
    }
}
