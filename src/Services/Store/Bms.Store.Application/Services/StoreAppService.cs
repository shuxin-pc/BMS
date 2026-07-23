using Mapster;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Bms.BuildingBlocks.Abstractions.Security;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Stores;
using StoreEntity = Bms.Store.Domain.Entities.Store;
using Bms.Store.Infrastructure;

namespace Bms.Store.Application.Services;

/// <summary>
/// 门店管理应用服务实现
/// </summary>
public class StoreAppService : IStoreAppService
{
    private readonly StoreDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<StoreCreateDto> _createValidator;
    private readonly IValidator<StoreUpdateDto> _updateValidator;

    public StoreAppService(
        StoreDbContext dbContext,
        ICurrentUser currentUser,
        IValidator<StoreCreateDto> createValidator,
        IValidator<StoreUpdateDto> updateValidator)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>
    /// 获取门店分页列表
    /// </summary>
    public async Task<ApiResponseDto<PagedResponseDto<StoreDto>>> GetPagedListAsync(StoreQueryDto query)
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return ApiResponseDto<PagedResponseDto<StoreDto>>.Fail("无法确定当前租户", 401);
        }

        var tenantId = _currentUser.TenantId.Value;
        var queryable = _dbContext.Stores
            .Where(s => !s.IsDeleted && s.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(query.Name))
        {
            queryable = queryable.Where(s => s.Name.Contains(query.Name));
        }
        if (!string.IsNullOrWhiteSpace(query.Code))
        {
            queryable = queryable.Where(s => s.Code.Contains(query.Code));
        }
        if (query.Status.HasValue)
        {
            queryable = queryable.Where(s => s.Status == query.Status.Value);
        }

        var total = await queryable.CountAsync();
        var items = await queryable
            .OrderByDescending(s => s.CreatedTime)
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var result = new PagedResponseDto<StoreDto>
        {
            List = items.Adapt<List<StoreDto>>(),
            Total = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
        return ApiResponseDto<PagedResponseDto<StoreDto>>.Ok(result);
    }

    /// <summary>
    /// 根据ID获取门店详情
    /// </summary>
    public async Task<ApiResponseDto<StoreDto?>> GetByIdAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return ApiResponseDto<StoreDto?>.Fail("无法确定当前租户", 401);
        }

        var store = await _dbContext.Stores
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted && s.TenantId == _currentUser.TenantId.Value);
        if (store == null)
        {
            return ApiResponseDto<StoreDto?>.Fail("门店不存在", 404);
        }
        return ApiResponseDto<StoreDto?>.Ok(store.Adapt<StoreDto>());
    }

    /// <summary>
    /// 获取当前租户下授权的门店列表（仅返回启用状态的门店，用于门店切换器）
    /// </summary>
    public async Task<ApiResponseDto<List<StoreDto>>> GetAuthorizedStoresAsync()
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return ApiResponseDto<List<StoreDto>>.Fail("无法确定当前租户", 401);
        }

        var tenantId = _currentUser.TenantId.Value;
        // 按 CreatedTime 升序排序：让最早创建的门店排第一位，配合前端 stores[0] 默认选中规则
        var stores = await _dbContext.Stores
            .Where(s => !s.IsDeleted && s.TenantId == tenantId && s.Status == 1)
            .OrderBy(s => s.CreatedTime)
            .ToListAsync();

        return ApiResponseDto<List<StoreDto>>.Ok(stores.Adapt<List<StoreDto>>());
    }

    /// <summary>
    /// 创建门店
    /// </summary>
    public async Task<ApiResponseDto<StoreDto>> CreateAsync(StoreCreateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return ApiResponseDto<StoreDto>.Fail("无法确定当前租户", 401);
        }

        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
        {
            return ApiResponseDto<StoreDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);
        }

        var tenantId = _currentUser.TenantId.Value;
        var codeExists = await _dbContext.Stores
            .AnyAsync(s => s.Code == dto.Code && s.TenantId == tenantId && !s.IsDeleted);
        if (codeExists)
        {
            return ApiResponseDto<StoreDto>.Fail($"门店编码 {dto.Code} 已存在", 400);
        }

        var store = dto.Adapt<StoreEntity>();
        store.TenantId = tenantId;
        store.TenantCode = _currentUser.TenantCode ?? string.Empty;
        store.CreatedTime = DateTime.Now;

        _dbContext.Stores.Add(store);
        await _dbContext.SaveChangesAsync();

        return ApiResponseDto<StoreDto>.Ok(store.Adapt<StoreDto>(), "创建成功");
    }

    /// <summary>
    /// 更新门店
    /// </summary>
    public async Task<ApiResponseDto<StoreDto>> UpdateAsync(StoreUpdateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return ApiResponseDto<StoreDto>.Fail("无法确定当前租户", 401);
        }

        var validation = await _updateValidator.ValidateAsync(dto);
        if (!validation.IsValid)
        {
            return ApiResponseDto<StoreDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);
        }

        var tenantId = _currentUser.TenantId.Value;
        var store = await _dbContext.Stores
            .FirstOrDefaultAsync(s => s.Id == dto.Id && !s.IsDeleted && s.TenantId == tenantId);
        if (store == null)
        {
            return ApiResponseDto<StoreDto>.Fail("门店不存在", 404);
        }

        // 编码变更时检查唯一性
        if (store.Code != dto.Code)
        {
            var codeExists = await _dbContext.Stores
                .AnyAsync(s => s.Code == dto.Code && s.TenantId == tenantId && !s.IsDeleted && s.Id != dto.Id);
            if (codeExists)
            {
                return ApiResponseDto<StoreDto>.Fail($"门店编码 {dto.Code} 已存在", 400);
            }
        }

        // 手动更新字段（避免覆盖 Id/TenantId 等审计字段）
        store.Name = dto.Name;
        store.Code = dto.Code;
        store.ShortName = dto.ShortName;
        store.Phone = dto.Phone;
        store.Address = dto.Address;
        store.BusinessHours = dto.BusinessHours;
        store.ManagerName = dto.ManagerName;
        store.Status = dto.Status;
        store.LogoUrl = dto.LogoUrl;
        store.BusinessLicenseUrl = dto.BusinessLicenseUrl;
        store.Remark = dto.Remark;
        store.UpdatedTime = DateTime.Now;

        await _dbContext.SaveChangesAsync();

        return ApiResponseDto<StoreDto>.Ok(store.Adapt<StoreDto>(), "更新成功");
    }

    /// <summary>
    /// 删除门店（软删除）
    /// </summary>
    public async Task<ApiResponseDto> DeleteAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return ApiResponseDto.Fail("无法确定当前租户", 401);
        }

        var store = await _dbContext.Stores
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted && s.TenantId == _currentUser.TenantId.Value);
        if (store == null)
        {
            return ApiResponseDto.Fail("门店不存在", 404);
        }

        store.IsDeleted = true;
        store.UpdatedTime = DateTime.Now;
        await _dbContext.SaveChangesAsync();

        return ApiResponseDto.Success(null, "删除成功");
    }

    /// <summary>
    /// 批量删除门店（软删除）
    /// </summary>
    public async Task<ApiResponseDto> BatchDeleteAsync(List<long> ids)
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return ApiResponseDto.Fail("无法确定当前租户", 401);
        }

        if (ids == null || !ids.Any())
        {
            return ApiResponseDto.Fail("请选择要删除的门店", 400);
        }

        var stores = await _dbContext.Stores
            .Where(s => ids.Contains(s.Id) && !s.IsDeleted && s.TenantId == _currentUser.TenantId.Value)
            .ToListAsync();

        foreach (var store in stores)
        {
            store.IsDeleted = true;
            store.UpdatedTime = DateTime.Now;
        }

        await _dbContext.SaveChangesAsync();

        return ApiResponseDto.Success(null, $"成功删除 {stores.Count} 个门店");
    }
}
