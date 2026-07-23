using Mapster;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Bms.BuildingBlocks.Abstractions.Security;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Products;
using ServiceProductEntity = Bms.Store.Domain.Entities.ServiceProduct;
using ServiceProductEquipmentEntity = Bms.Store.Domain.Entities.ServiceProductEquipment;
using Bms.Store.Infrastructure;

namespace Bms.Store.Application.Services;

/// <summary>
/// 服务商品子表应用服务实现
/// </summary>
public class ServiceProductAppService : IServiceProductAppService
{
    private readonly StoreDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<ServiceProductCreateDto> _createValidator;
    private readonly IValidator<ServiceProductUpdateDto> _updateValidator;

    public ServiceProductAppService(
        StoreDbContext dbContext,
        ICurrentUser currentUser,
        IValidator<ServiceProductCreateDto> createValidator,
        IValidator<ServiceProductUpdateDto> updateValidator)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>
    /// 获取服务商品子表分页列表
    /// </summary>
    public async Task<ApiResponseDto<PagedResponseDto<ServiceProductDto>>> GetPagedListAsync(ServiceProductQueryDto query)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<PagedResponseDto<ServiceProductDto>>.Fail("无法确定当前租户", 401);

        var tenantId = _currentUser.TenantId.Value;
        var queryable = _dbContext.ServiceProducts
            .Where(p => !p.IsDeleted && p.TenantId == tenantId);

        if (query.ProductId.HasValue)
            queryable = queryable.Where(p => p.ProductId == query.ProductId.Value);

        var total = await queryable.CountAsync();
        var items = await queryable
            .OrderByDescending(p => p.CreatedTime)
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var dtos = items.Adapt<List<ServiceProductDto>>();

        // 批量填充所需设备类型 ID + 名称
        var serviceIds = items.Select(s => s.Id).ToList();
        if (serviceIds.Any())
        {
            var relations = await _dbContext.ServiceProductEquipments
                .Where(r => serviceIds.Contains(r.ServiceProductId) && !r.IsDeleted)
                .Join(_dbContext.EquipmentTypes,
                    r => r.EquipmentTypeId,
                    e => e.Id,
                    (r, e) => new { r.ServiceProductId, EquipmentTypeId = r.EquipmentTypeId, EquipmentTypeName = e.Name })
                .ToListAsync();
            var relationsByService = relations.GroupBy(x => x.ServiceProductId)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var dto in dtos)
            {
                if (relationsByService.TryGetValue(dto.Id, out var list))
                {
                    dto.EquipmentTypeIds = list.Select(x => x.EquipmentTypeId).ToList();
                    dto.EquipmentTypeNames = list.Select(x => x.EquipmentTypeName).ToList();
                }
            }
        }

        var result = new PagedResponseDto<ServiceProductDto>
        {
            List = dtos,
            Total = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
        return ApiResponseDto<PagedResponseDto<ServiceProductDto>>.Ok(result);
    }

    /// <summary>
    /// 根据ID获取服务商品子表详情
    /// </summary>
    public async Task<ApiResponseDto<ServiceProductDto?>> GetByIdAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<ServiceProductDto?>.Fail("无法确定当前租户", 401);

        var entity = await _dbContext.ServiceProducts
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted && p.TenantId == _currentUser.TenantId.Value);
        if (entity == null)
            return ApiResponseDto<ServiceProductDto?>.Fail("服务商品不存在", 404);

        var dto = entity.Adapt<ServiceProductDto>();

        // 填充所需设备类型
        var relations = await _dbContext.ServiceProductEquipments
            .Where(r => r.ServiceProductId == entity.Id && !r.IsDeleted)
            .Join(_dbContext.EquipmentTypes,
                r => r.EquipmentTypeId,
                e => e.Id,
                (r, e) => new { EquipmentTypeId = r.EquipmentTypeId, EquipmentTypeName = e.Name })
            .ToListAsync();
        dto.EquipmentTypeIds = relations.Select(x => x.EquipmentTypeId).ToList();
        dto.EquipmentTypeNames = relations.Select(x => x.EquipmentTypeName).ToList();

        return ApiResponseDto<ServiceProductDto?>.Ok(dto);
    }

    /// <summary>
    /// 创建服务商品子表
    /// </summary>
    public async Task<ApiResponseDto<ServiceProductDto>> CreateAsync(ServiceProductCreateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<ServiceProductDto>.Fail("无法确定当前租户", 401);

        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<ServiceProductDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var tenantCode = _currentUser.TenantCode ?? string.Empty;
        var storeId = _currentUser.StoreId ?? 0;

        var entity = dto.Adapt<ServiceProductEntity>();
        entity.TenantId = tenantId;
        entity.TenantCode = tenantCode;
        entity.StoreId = storeId;
        entity.StoreCode = "";
        entity.CreatedTime = DateTime.Now;
        // Mapster 会把 dto.EquipmentTypeIds 映射到 entity.RequiredEquipmentTypes（集合导航），需清空避免误用
        entity.RequiredEquipmentTypes = new();

        _dbContext.ServiceProducts.Add(entity);
        await _dbContext.SaveChangesAsync();

        // 级联创建所需设备类型关联
        if (dto.EquipmentTypeIds != null && dto.EquipmentTypeIds.Any())
        {
            foreach (var equipmentTypeId in dto.EquipmentTypeIds.Distinct())
            {
                _dbContext.ServiceProductEquipments.Add(new ServiceProductEquipmentEntity
                {
                    ServiceProductId = entity.Id,
                    EquipmentTypeId = equipmentTypeId,
                    TenantId = tenantId,
                    TenantCode = tenantCode,
                    StoreId = storeId,
                    StoreCode = "",
                    CreatedTime = DateTime.Now
                });
            }
            await _dbContext.SaveChangesAsync();
        }

        var resultDto = entity.Adapt<ServiceProductDto>();
        resultDto.EquipmentTypeIds = dto.EquipmentTypeIds?.Distinct().ToList() ?? new();
        return ApiResponseDto<ServiceProductDto>.Ok(resultDto, "创建成功");
    }

    /// <summary>
    /// 更新服务商品子表
    /// </summary>
    public async Task<ApiResponseDto<ServiceProductDto>> UpdateAsync(ServiceProductUpdateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<ServiceProductDto>.Fail("无法确定当前租户", 401);

        var validation = await _updateValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<ServiceProductDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var entity = await _dbContext.ServiceProducts
            .FirstOrDefaultAsync(p => p.Id == dto.Id && !p.IsDeleted && p.TenantId == tenantId);
        if (entity == null)
            return ApiResponseDto<ServiceProductDto>.Fail("服务商品不存在", 404);

        entity.ProductId = dto.ProductId;
        entity.Duration = dto.Duration;
        entity.RequiredRoomType = dto.RequiredRoomType;
        entity.ApplicableSkills = dto.ApplicableSkills;
        entity.UpdatedTime = DateTime.Now;

        // 替换所需设备类型关联：软删除旧记录 + 创建新记录
        var oldRelations = await _dbContext.ServiceProductEquipments
            .Where(r => r.ServiceProductId == entity.Id && !r.IsDeleted)
            .ToListAsync();
        foreach (var old in oldRelations)
        {
            old.IsDeleted = true;
            old.UpdatedTime = DateTime.Now;
        }

        if (dto.EquipmentTypeIds != null && dto.EquipmentTypeIds.Any())
        {
            var tenantCode = _currentUser.TenantCode ?? string.Empty;
            var storeId = _currentUser.StoreId ?? 0;
            foreach (var equipmentTypeId in dto.EquipmentTypeIds.Distinct())
            {
                _dbContext.ServiceProductEquipments.Add(new ServiceProductEquipmentEntity
                {
                    ServiceProductId = entity.Id,
                    EquipmentTypeId = equipmentTypeId,
                    TenantId = tenantId,
                    TenantCode = tenantCode,
                    StoreId = storeId,
                    StoreCode = "",
                    CreatedTime = DateTime.Now
                });
            }
        }

        await _dbContext.SaveChangesAsync();

        var resultDto = entity.Adapt<ServiceProductDto>();
        resultDto.EquipmentTypeIds = dto.EquipmentTypeIds?.Distinct().ToList() ?? new();
        return ApiResponseDto<ServiceProductDto>.Ok(resultDto, "更新成功");
    }

    /// <summary>
    /// 删除服务商品子表（软删除）
    /// </summary>
    public async Task<ApiResponseDto> DeleteAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("无法确定当前租户", 401);

        var entity = await _dbContext.ServiceProducts
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted && p.TenantId == _currentUser.TenantId.Value);
        if (entity == null)
            return ApiResponseDto.Fail("服务商品不存在", 404);

        entity.IsDeleted = true;
        entity.UpdatedTime = DateTime.Now;

        // 级联软删除所需设备类型关联
        var relations = await _dbContext.ServiceProductEquipments
            .Where(r => r.ServiceProductId == entity.Id && !r.IsDeleted)
            .ToListAsync();
        foreach (var r in relations)
        {
            r.IsDeleted = true;
            r.UpdatedTime = DateTime.Now;
        }

        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, "删除成功");
    }

    /// <summary>
    /// 批量删除服务商品子表（软删除）
    /// </summary>
    public async Task<ApiResponseDto> BatchDeleteAsync(List<long> ids)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("无法确定当前租户", 401);
        if (ids == null || !ids.Any())
            return ApiResponseDto.Fail("请选择要删除的数据", 400);

        var entities = await _dbContext.ServiceProducts
            .Where(p => ids.Contains(p.Id) && !p.IsDeleted && p.TenantId == _currentUser.TenantId.Value)
            .ToListAsync();

        foreach (var entity in entities)
        {
            entity.IsDeleted = true;
            entity.UpdatedTime = DateTime.Now;
        }

        // 级联软删除所需设备类型关联
        var entityIds = entities.Select(e => e.Id).ToList();
        var relations = await _dbContext.ServiceProductEquipments
            .Where(r => entityIds.Contains(r.ServiceProductId) && !r.IsDeleted)
            .ToListAsync();
        foreach (var r in relations)
        {
            r.IsDeleted = true;
            r.UpdatedTime = DateTime.Now;
        }

        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, $"成功删除 {entities.Count} 条数据");
    }
}
