using Bms.BuildingBlocks.MultiTenant.Abstractions;
using Bms.BuildingBlocks.MultiTenant.Models;
using Bms.System.Application.Dtos;
using Bms.System.Application.Dtos.Tenants;

namespace Bms.System.Application.Services;

/// <summary>
/// 租户管理应用服务实现
/// </summary>
public class TenantAppService : ITenantAppService
{
    private readonly ITenantStore _tenantStore;

    public TenantAppService(ITenantStore tenantStore)
    {
        _tenantStore = tenantStore;
    }

    public async Task<ApiResponseDto<PagedResponseDto<TenantDto>>> GetPagedListAsync(PagedRequestDto request)
    {
        // 获取所有租户
        var allTenants = await _tenantStore.GetAllTenantsAsync();

        // 应用筛选条件
        var filteredTenants = allTenants.AsEnumerable();

        // 按租户名称筛选
        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            filteredTenants = filteredTenants.Where(t => t.Name != null && t.Name.Contains(request.Name, StringComparison.OrdinalIgnoreCase));
        }

        // 按租户编码筛选
        if (!string.IsNullOrWhiteSpace(request.Code))
        {
            filteredTenants = filteredTenants.Where(t => t.Code != null && t.Code.Contains(request.Code, StringComparison.OrdinalIgnoreCase));
        }

        // 按状态筛选
        if (request.Status.HasValue)
        {
            filteredTenants = filteredTenants.Where(t => t.Status == request.Status.Value);
        }

        var filteredList = filteredTenants.ToList();
        var totalCount = filteredList.Count;
        var pagedTenants = filteredList
            .Skip((request.PageIndex - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        var tenantDtos = pagedTenants.Select(t => MapToDto(t)).ToList();

        var result = new PagedResponseDto<TenantDto>
        {
            List = tenantDtos,
            Total = totalCount,
            PageIndex = request.PageIndex,
            PageSize = request.PageSize
        };

        return ApiResponseDto<PagedResponseDto<TenantDto>>.Success(result);
    }

    public async Task<ApiResponseDto<TenantDto?>> GetByIdAsync(long id)
    {
        var tenant = await _tenantStore.GetTenantByIdAsync(id);
        if (tenant == null)
        {
            return ApiResponseDto<TenantDto?>.Fail("租户不存在", 404);
        }
        return ApiResponseDto<TenantDto?>.Success(MapToDto(tenant));
    }

    public async Task<ApiResponseDto<TenantDto?>> GetByCodeAsync(string code)
    {
        var tenant = await _tenantStore.GetTenantByCodeAsync(code);
        if (tenant == null)
        {
            return ApiResponseDto<TenantDto?>.Fail("租户不存在", 404);
        }
        return ApiResponseDto<TenantDto?>.Success(MapToDto(tenant));
    }

    public async Task<ApiResponseDto<TenantDto>> CreateAsync(TenantCreateDto dto)
    {
        // 检查租户编码是否已存在
        var existing = await _tenantStore.GetTenantByCodeAsync(dto.Code);
        if (existing != null)
        {
            throw new InvalidOperationException($"租户编码 {dto.Code} 已存在");
        }

        var tenant = new TenantInfo
        {
            Id = 0, // 实际保存时由数据库生成
            Code = dto.Code,
            Name = dto.Name,
            ContactName = dto.ContactName,
            ContactPhone = dto.ContactPhone,
            ContactEmail = dto.ContactEmail,
            Status = dto.Status,
            IsolationLevel = dto.IsolationLevel,
            ConnectionString = dto.ConnectionString,
            SchemaName = dto.SchemaName ?? dto.Code.ToLowerInvariant(),
            IsEnabled = dto.IsEnabled,
            ExpireTime = dto.ExpireTime,
            AllowedSubsystems = dto.AllowedSubsystems,
            Remark = dto.Remark
        };

        // 保存到数据库
        var newId = await _tenantStore.AddTenantAsync(tenant);
        if (newId <= 0)
        {
            throw new InvalidOperationException("创建失败");
        }
        tenant.Id = newId;

        // 创建默认Schema（实际实现中需要执行数据库命令）
        // await CreateTenantSchemaAsync(tenant);

        return ApiResponseDto<TenantDto>.Success(MapToDto(tenant), "创建成功");
    }

    public async Task<ApiResponseDto<TenantDto>> UpdateAsync(TenantUpdateDto dto)
    {
        var tenant = await _tenantStore.GetTenantByIdAsync(dto.Id);
        if (tenant == null)
        {
            throw new InvalidOperationException("租户不存在");
        }

        // 更新租户基本信息
        tenant.Name = dto.Name;
        tenant.ContactName = dto.ContactName;
        tenant.ContactPhone = dto.ContactPhone;
        tenant.ContactEmail = dto.ContactEmail;
        tenant.Status = dto.Status;
        tenant.IsolationLevel = dto.IsolationLevel;
        tenant.ConnectionString = dto.ConnectionString;
        tenant.SchemaName = dto.SchemaName;
        tenant.IsEnabled = dto.IsEnabled;
        tenant.ExpireTime = dto.ExpireTime;
        tenant.AllowedSubsystems = dto.AllowedSubsystems;
        tenant.Remark = dto.Remark;

        // 保存到数据库
        var updated = await _tenantStore.UpdateTenantAsync(tenant);
        if (!updated)
        {
            throw new InvalidOperationException("更新失败");
        }

        return ApiResponseDto<TenantDto>.Success(MapToDto(tenant), "更新成功");
    }

    public async Task<ApiResponseDto> DeleteAsync(long id)
    {
        var tenant = await _tenantStore.GetTenantByIdAsync(id);
        if (tenant == null)
        {
            throw new InvalidOperationException("租户不存在");
        }

        // 从数据库删除
        var deleted = await _tenantStore.DeleteTenantAsync(id);
        if (!deleted)
        {
            throw new InvalidOperationException("删除失败");
        }

        // 删除租户Schema（实际实现中需要执行数据库命令）
        // await DropTenantSchemaAsync(id);

        return ApiResponseDto.Success(null, "删除成功");
    }

    public async Task<ApiResponseDto> BatchDeleteAsync(List<long> ids)
    {
        if (ids == null || !ids.Any())
        {
            throw new InvalidOperationException("请选择要删除的租户");
        }

        var deletedCount = await _tenantStore.BatchDeleteAsync(ids);
        if (deletedCount == 0)
        {
            throw new InvalidOperationException("删除失败");
        }

        return ApiResponseDto.Success(null, $"成功删除 {deletedCount} 个租户");
    }

    public async Task<ApiResponseDto> EnableAsync(long id)
    {
        var tenant = await _tenantStore.GetTenantByIdAsync(id);
        if (tenant == null)
        {
            throw new InvalidOperationException("租户不存在");
        }

        tenant.IsEnabled = true;
        tenant.Status = 1;
        var updated = await _tenantStore.UpdateTenantAsync(tenant);
        if (!updated)
        {
            throw new InvalidOperationException("启用失败");
        }

        return ApiResponseDto.Success(null, "启用成功");
    }

    public async Task<ApiResponseDto> DisableAsync(long id)
    {
        var tenant = await _tenantStore.GetTenantByIdAsync(id);
        if (tenant == null)
        {
            throw new InvalidOperationException("租户不存在");
        }

        tenant.IsEnabled = false;
        tenant.Status = 0;
        var updated = await _tenantStore.UpdateTenantAsync(tenant);
        if (!updated)
        {
            throw new InvalidOperationException("禁用失败");
        }

        return ApiResponseDto.Success(null, "禁用成功");
    }

    private TenantDto MapToDto(TenantInfo tenant)
    {
        return new TenantDto
        {
            Id = tenant.Id,
            Code = tenant.Code,
            Name = tenant.Name,
            ContactName = tenant.ContactName,
            ContactPhone = tenant.ContactPhone,
            ContactEmail = tenant.ContactEmail,
            Status = tenant.Status,
            IsolationLevel = tenant.IsolationLevel,
            ConnectionString = tenant.ConnectionString,
            SchemaName = tenant.SchemaName,
            IsEnabled = tenant.IsEnabled,
            ExpireTime = tenant.ExpireTime,
            AllowedSubsystems = tenant.AllowedSubsystems,
            AllowedSubsystemList = tenant.GetAllowedSubsystemList(),
            CreatedAt = DateTime.UtcNow,
            Remark = tenant.Remark
        };
    }
}
