using Mapster;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Bms.BuildingBlocks.Abstractions.Security;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.ServiceBoms;
using ServiceBomEntity = Bms.Store.Domain.Entities.ServiceBom;
using Bms.Store.Infrastructure;

namespace Bms.Store.Application.Services;

/// <summary>
/// 服务BOM应用服务实现
/// </summary>
public class ServiceBomAppService : IServiceBomAppService
{
    private readonly StoreDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<ServiceBomCreateDto> _createValidator;
    private readonly IValidator<ServiceBomUpdateDto> _updateValidator;

    public ServiceBomAppService(
        StoreDbContext dbContext,
        ICurrentUser currentUser,
        IValidator<ServiceBomCreateDto> createValidator,
        IValidator<ServiceBomUpdateDto> updateValidator)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<ApiResponseDto<PagedResponseDto<ServiceBomDto>>> GetPagedListAsync(ServiceBomQueryDto query)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<PagedResponseDto<ServiceBomDto>>.Fail("无法确定当前租户", 401);

        var tenantId = _currentUser.TenantId.Value;
        var queryable = _dbContext.ServiceBoms
            .Where(b => !b.IsDeleted && b.TenantId == tenantId);

        if (query.ServiceProductId.HasValue)
            queryable = queryable.Where(b => b.ServiceProductId == query.ServiceProductId.Value);
        if (query.ConsumableProductId.HasValue)
            queryable = queryable.Where(b => b.ConsumableProductId == query.ConsumableProductId.Value);

        var total = await queryable.CountAsync();
        var items = await queryable
            .OrderByDescending(b => b.CreatedTime)
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var result = new PagedResponseDto<ServiceBomDto>
        {
            List = items.Adapt<List<ServiceBomDto>>(),
            Total = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
        return ApiResponseDto<PagedResponseDto<ServiceBomDto>>.Ok(result);
    }

    public async Task<ApiResponseDto<ServiceBomDto?>> GetByIdAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<ServiceBomDto?>.Fail("无法确定当前租户", 401);

        var bom = await _dbContext.ServiceBoms
            .FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted && b.TenantId == _currentUser.TenantId.Value);
        if (bom == null)
            return ApiResponseDto<ServiceBomDto?>.Fail("服务BOM不存在", 404);
        return ApiResponseDto<ServiceBomDto?>.Ok(bom.Adapt<ServiceBomDto>());
    }

    public async Task<ApiResponseDto<ServiceBomDto>> CreateAsync(ServiceBomCreateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<ServiceBomDto>.Fail("无法确定当前租户", 401);

        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<ServiceBomDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var exists = await _dbContext.ServiceBoms
            .AnyAsync(b => b.ServiceProductId == dto.ServiceProductId
                && b.ConsumableProductId == dto.ConsumableProductId
                && b.TenantId == tenantId
                && !b.IsDeleted);
        if (exists)
            return ApiResponseDto<ServiceBomDto>.Fail("该服务项目已存在此耗材BOM记录", 400);

        var bom = dto.Adapt<ServiceBomEntity>();
        bom.TenantId = tenantId;
        bom.TenantCode = _currentUser.TenantCode ?? string.Empty;
        bom.CreatedTime = DateTime.Now;

        _dbContext.ServiceBoms.Add(bom);
        await _dbContext.SaveChangesAsync();

        return ApiResponseDto<ServiceBomDto>.Ok(bom.Adapt<ServiceBomDto>(), "创建成功");
    }

    public async Task<ApiResponseDto<ServiceBomDto>> UpdateAsync(ServiceBomUpdateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<ServiceBomDto>.Fail("无法确定当前租户", 401);

        var validation = await _updateValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<ServiceBomDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var bom = await _dbContext.ServiceBoms
            .FirstOrDefaultAsync(b => b.Id == dto.Id && !b.IsDeleted && b.TenantId == tenantId);
        if (bom == null)
            return ApiResponseDto<ServiceBomDto>.Fail("服务BOM不存在", 404);

        if (bom.ServiceProductId != dto.ServiceProductId || bom.ConsumableProductId != dto.ConsumableProductId)
        {
            var exists = await _dbContext.ServiceBoms
                .AnyAsync(b => b.ServiceProductId == dto.ServiceProductId
                    && b.ConsumableProductId == dto.ConsumableProductId
                    && b.TenantId == tenantId
                    && !b.IsDeleted
                    && b.Id != dto.Id);
            if (exists)
                return ApiResponseDto<ServiceBomDto>.Fail("该服务项目已存在此耗材BOM记录", 400);
        }

        bom.ServiceProductId = dto.ServiceProductId;
        bom.ConsumableProductId = dto.ConsumableProductId;
        bom.Quantity = dto.Quantity;
        bom.UpdatedTime = DateTime.Now;

        await _dbContext.SaveChangesAsync();
        return ApiResponseDto<ServiceBomDto>.Ok(bom.Adapt<ServiceBomDto>(), "更新成功");
    }

    public async Task<ApiResponseDto> DeleteAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("无法确定当前租户", 401);

        var bom = await _dbContext.ServiceBoms
            .FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted && b.TenantId == _currentUser.TenantId.Value);
        if (bom == null)
            return ApiResponseDto.Fail("服务BOM不存在", 404);

        bom.IsDeleted = true;
        bom.UpdatedTime = DateTime.Now;
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, "删除成功");
    }

    public async Task<ApiResponseDto> BatchDeleteAsync(List<long> ids)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("无法确定当前租户", 401);
        if (ids == null || !ids.Any())
            return ApiResponseDto.Fail("请选择要删除的数据", 400);

        var boms = await _dbContext.ServiceBoms
            .Where(b => ids.Contains(b.Id) && !b.IsDeleted && b.TenantId == _currentUser.TenantId.Value)
            .ToListAsync();

        foreach (var bom in boms)
        {
            bom.IsDeleted = true;
            bom.UpdatedTime = DateTime.Now;
        }
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, $"成功删除 {boms.Count} 条数据");
    }
}
