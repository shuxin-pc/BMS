using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bms.System.Application.Dtos;
using Bms.System.Application.Dtos.Permissions;
using Bms.System.Application.Services;

namespace Bms.System.Api.Controllers;

[ApiController]
[Route("api/system/[controller]")]
[Route("api/[controller]")]
[Authorize]
public class PermissionsController : ControllerBase
{
    private readonly IPermissionAppService _permissionService;

    public PermissionsController(IPermissionAppService permissionService)
    {
        _permissionService = permissionService;
    }

    /// <summary>
    /// 获取权限分页列表
    /// </summary>
    [HttpGet]
    public async Task<ApiResponseDto<PagedResponseDto<PermissionDto>>> GetList([FromQuery] PagedRequestDto request)
    {
        var result = await _permissionService.GetPagedListAsync(request);
        return ApiResponseDto<PagedResponseDto<PermissionDto>>.Success(result);
    }

    /// <summary>
    /// 获取菜单的权限列表
    /// </summary>
    [HttpGet("menu/{menuId}")]
    public async Task<ApiResponseDto<List<PermissionDto>>> GetByMenuId(long menuId)
    {
        var result = await _permissionService.GetListByMenuIdAsync(menuId);
        return ApiResponseDto<List<PermissionDto>>.Success(result);
    }

    /// <summary>
    /// 获取权限详情
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ApiResponseDto<PermissionDto>> GetById(long id)
    {
        var result = await _permissionService.GetByIdAsync(id);
        return ApiResponseDto<PermissionDto>.Success(result);
    }

    /// <summary>
    /// 创建权限
    /// </summary>
    [HttpPost]
    public async Task<ApiResponseDto<PermissionDto>> Create([FromBody] PermissionCreateDto dto)
    {
        try
        {
            var result = await _permissionService.CreateAsync(dto);
            return ApiResponseDto<PermissionDto>.Success(result, "创建成功");
        }
        catch (InvalidOperationException ex)
        {
            return ApiResponseDto<PermissionDto>.Fail(ex.Message, 400);
        }
    }

    /// <summary>
    /// 更新权限
    /// </summary>
    [HttpPut]
    public async Task<ApiResponseDto<PermissionDto>> Update([FromBody] PermissionUpdateDto dto)
    {
        try
        {
            var result = await _permissionService.UpdateAsync(dto);
            return ApiResponseDto<PermissionDto>.Success(result, "更新成功");
        }
        catch (InvalidOperationException ex)
        {
            return ApiResponseDto<PermissionDto>.Fail(ex.Message, 400);
        }
    }

    /// <summary>
    /// 删除权限
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ApiResponseDto> Delete(long id)
    {
        await _permissionService.DeleteAsync(id);
        return ApiResponseDto.Success(null, "删除成功");
    }
}
