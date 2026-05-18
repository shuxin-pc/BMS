using Mapster;
using Bms.System.Application.Dtos;
using Bms.System.Application.Dtos.DataPermissions;
using Bms.System.Domain.Entities;
using Bms.System.Domain.IRepositories;
using Bms.System.Domain.Enums;

namespace Bms.System.Application.Services;

public class DataPermissionAppService : IDataPermissionAppService
{
    private readonly IDataPermissionRepository _dataPermissionRepository;

    public DataPermissionAppService(IDataPermissionRepository dataPermissionRepository)
    {
        _dataPermissionRepository = dataPermissionRepository;
    }

    public async Task<ApiResponseDto<DataPermissionDto?>> GetByIdAsync(long id)
    {
        var dataPermission = await _dataPermissionRepository.GetByIdAsync(id);
        if (dataPermission == null)
        {
            return ApiResponseDto<DataPermissionDto?>.Fail("数据权限不存在", 404);
        }
        return ApiResponseDto<DataPermissionDto?>.Success(dataPermission.Adapt<DataPermissionDto>());
    }

    public async Task<ApiResponseDto<DataPermissionDto?>> GetByRoleIdAsync(long roleId)
    {
        var dataPermission = await _dataPermissionRepository.GetByRoleIdAsync(roleId);
        if (dataPermission == null)
        {
            return ApiResponseDto<DataPermissionDto?>.Fail("数据权限不存在", 404);
        }
        return ApiResponseDto<DataPermissionDto?>.Success(dataPermission.Adapt<DataPermissionDto>());
    }

    public async Task<ApiResponseDto<List<DataPermissionDto>>> GetListAsync()
    {
        var dataPermissions = await _dataPermissionRepository.GetListAsync();
        return ApiResponseDto<List<DataPermissionDto>>.Success(dataPermissions.Select(d => d.Adapt<DataPermissionDto>()).ToList());
    }

    public async Task<ApiResponseDto<DataPermissionDto>> CreateAsync(DataPermissionCreateDto dto)
    {
        // 检查角色是否已有数据权限
        var existing = await _dataPermissionRepository.GetByRoleIdAsync(dto.RoleId);
        if (existing != null)
        {
            throw new InvalidOperationException("该角色已存在数据权限配置");
        }

        // 验证自定义组织ID
        if (dto.DataScopeType == DataScopeType.Custom && string.IsNullOrEmpty(dto.CustomOrganizationIds))
        {
            throw new InvalidOperationException("自定义数据范围时必须指定组织");
        }

        var dataPermission = new DataPermission
        {
            RoleId = dto.RoleId,
            DataScopeType = (int)dto.DataScopeType,
            CustomOrganizationIds = dto.CustomOrganizationIds
        };

        await _dataPermissionRepository.AddAsync(dataPermission);
        var created = await _dataPermissionRepository.GetByIdAsync(dataPermission.Id);
        if (created == null)
        {
            throw new InvalidOperationException("创建数据权限失败");
        }
        return ApiResponseDto<DataPermissionDto>.Success(created.Adapt<DataPermissionDto>(), "创建成功");
    }

    public async Task<ApiResponseDto<DataPermissionDto>> UpdateAsync(DataPermissionUpdateDto dto)
    {
        var dataPermission = await _dataPermissionRepository.GetByIdAsync(dto.Id);
        if (dataPermission == null)
        {
            throw new InvalidOperationException("数据权限不存在");
        }

        // 验证自定义组织ID
        if (dto.DataScopeType == DataScopeType.Custom && string.IsNullOrEmpty(dto.CustomOrganizationIds))
        {
            throw new InvalidOperationException("自定义数据范围时必须指定组织");
        }

        dataPermission.DataScopeType = (int)dto.DataScopeType;
        dataPermission.CustomOrganizationIds = dto.CustomOrganizationIds;

        await _dataPermissionRepository.UpdateAsync(dataPermission);
        var updated = await _dataPermissionRepository.GetByIdAsync(dataPermission.Id);
        if (updated == null)
        {
            throw new InvalidOperationException("更新数据权限失败");
        }
        return ApiResponseDto<DataPermissionDto>.Success(updated.Adapt<DataPermissionDto>(), "更新成功");
    }

    public async Task<ApiResponseDto> DeleteAsync(long id)
    {
        await _dataPermissionRepository.DeleteAsync(id);
        return ApiResponseDto.Success(null, "删除成功");
    }

    public async Task<ApiResponseDto> DeleteByRoleIdAsync(long roleId)
    {
        await _dataPermissionRepository.DeleteByRoleIdAsync(roleId);
        return ApiResponseDto.Success(null, "删除成功");
    }
}
