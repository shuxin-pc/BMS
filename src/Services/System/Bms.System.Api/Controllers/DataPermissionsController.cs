using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bms.System.Application.Dtos;
using Bms.System.Application.Dtos.DataPermissions;
using Bms.System.Application.Services;

namespace Bms.System.Api.Controllers;

[ApiController]
[Route("api/system/[controller]")]
[Route("api/[controller]")]
[Authorize]
public class DataPermissionsController : ControllerBase
{
    private readonly IDataPermissionAppService _dataPermissionService;

    public DataPermissionsController(IDataPermissionAppService dataPermissionService)
    {
        _dataPermissionService = dataPermissionService;
    }

    /// <summary>
    /// 获取数据权限详情
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ApiResponseDto<DataPermissionDto?>> GetById(long id)
    {
        return await _dataPermissionService.GetByIdAsync(id);
    }

    /// <summary>
    /// 根据角色ID获取数据权限
    /// </summary>
    [HttpGet("role/{roleId}")]
    public async Task<ApiResponseDto<DataPermissionDto?>> GetByRoleId(long roleId)
    {
        return await _dataPermissionService.GetByRoleIdAsync(roleId);
    }

    /// <summary>
    /// 获取所有数据权限列表
    /// </summary>
    [HttpGet]
    public async Task<ApiResponseDto<List<DataPermissionDto>>> GetList()
    {
        return await _dataPermissionService.GetListAsync();
    }

    /// <summary>
    /// 创建数据权限
    /// </summary>
    [HttpPost]
    public async Task<ApiResponseDto<DataPermissionDto>> Create([FromBody] DataPermissionCreateDto dto)
    {
        try
        {
            return await _dataPermissionService.CreateAsync(dto);
        }
        catch (InvalidOperationException ex)
        {
            return ApiResponseDto<DataPermissionDto>.Fail(ex.Message, 400);
        }
    }

    /// <summary>
    /// 更新数据权限
    /// </summary>
    [HttpPut]
    public async Task<ApiResponseDto<DataPermissionDto>> Update([FromBody] DataPermissionUpdateDto dto)
    {
        try
        {
            return await _dataPermissionService.UpdateAsync(dto);
        }
        catch (InvalidOperationException ex)
        {
            return ApiResponseDto<DataPermissionDto>.Fail(ex.Message, 400);
        }
    }

    /// <summary>
    /// 删除数据权限
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ApiResponseDto> Delete(long id)
    {
        return await _dataPermissionService.DeleteAsync(id);
    }

    /// <summary>
    /// 根据角色ID删除数据权限
    /// </summary>
    [HttpDelete("role/{roleId}")]
    public async Task<ApiResponseDto> DeleteByRoleId(long roleId)
    {
        return await _dataPermissionService.DeleteByRoleIdAsync(roleId);
    }
}
