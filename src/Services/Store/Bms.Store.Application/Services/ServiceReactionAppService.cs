using Mapster;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Bms.BuildingBlocks.Abstractions.Security;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Customers;
using ServiceReactionEntity = Bms.Store.Domain.Entities.ServiceReaction;
using Bms.Store.Infrastructure;

namespace Bms.Store.Application.Services;

public class ServiceReactionAppService : IServiceReactionAppService
{
    private readonly StoreDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<ServiceReactionCreateDto> _createValidator;
    private readonly IValidator<ServiceReactionUpdateDto> _updateValidator;

    public ServiceReactionAppService(
        StoreDbContext dbContext,
        ICurrentUser currentUser,
        IValidator<ServiceReactionCreateDto> createValidator,
        IValidator<ServiceReactionUpdateDto> updateValidator)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<ApiResponseDto<PagedResponseDto<ServiceReactionDto>>> GetPagedListAsync(ServiceReactionQueryDto query)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<PagedResponseDto<ServiceReactionDto>>.Fail("无法确定当前租户", 401);

        var tenantId = _currentUser.TenantId.Value;
        var queryable = _dbContext.ServiceReactions
            .Where(p => p.TenantId == tenantId);

        if (query.CustomerId.HasValue)
            queryable = queryable.Where(p => p.CustomerId == query.CustomerId.Value);

        var total = await queryable.CountAsync();
        var items = await queryable
            .OrderByDescending(p => p.CreatedTime)
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var result = new PagedResponseDto<ServiceReactionDto>
        {
            List = items.Adapt<List<ServiceReactionDto>>(),
            Total = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
        return ApiResponseDto<PagedResponseDto<ServiceReactionDto>>.Ok(result);
    }

    public async Task<ApiResponseDto<ServiceReactionDto?>> GetByIdAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<ServiceReactionDto?>.Fail("无法确定当前租户", 401);

        var entity = await _dbContext.ServiceReactions
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == _currentUser.TenantId.Value);
        if (entity == null)
            return ApiResponseDto<ServiceReactionDto?>.Fail("服务反应记录不存在", 404);
        return ApiResponseDto<ServiceReactionDto?>.Ok(entity.Adapt<ServiceReactionDto>());
    }

    public async Task<ApiResponseDto<ServiceReactionDto>> CreateAsync(ServiceReactionCreateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<ServiceReactionDto>.Fail("无法确定当前租户", 401);

        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<ServiceReactionDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var entity = dto.Adapt<ServiceReactionEntity>();
        entity.TenantId = tenantId;
        entity.TenantCode = _currentUser.TenantCode ?? string.Empty;
        entity.CreatedTime = DateTime.Now;

        _dbContext.ServiceReactions.Add(entity);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto<ServiceReactionDto>.Ok(entity.Adapt<ServiceReactionDto>(), "创建成功");
    }

    public async Task<ApiResponseDto<ServiceReactionDto>> UpdateAsync(ServiceReactionUpdateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<ServiceReactionDto>.Fail("无法确定当前租户", 401);

        var validation = await _updateValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<ServiceReactionDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var entity = await _dbContext.ServiceReactions
            .FirstOrDefaultAsync(p => p.Id == dto.Id && p.TenantId == tenantId);
        if (entity == null)
            return ApiResponseDto<ServiceReactionDto>.Fail("服务反应记录不存在", 404);

        entity.CustomerId = dto.CustomerId;
        entity.OrderId = dto.OrderId;
        entity.ServiceItem = dto.ServiceItem;
        entity.ReactionDate = dto.ReactionDate;
        entity.Reaction = dto.Reaction;
        entity.Severity = dto.Severity;
        entity.Remark = dto.Remark;
        entity.UpdatedTime = DateTime.Now;

        await _dbContext.SaveChangesAsync();
        return ApiResponseDto<ServiceReactionDto>.Ok(entity.Adapt<ServiceReactionDto>(), "更新成功");
    }

    public async Task<ApiResponseDto> DeleteAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("无法确定当前租户", 401);

        var entity = await _dbContext.ServiceReactions
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == _currentUser.TenantId.Value);
        if (entity == null)
            return ApiResponseDto.Fail("服务反应记录不存在", 404);

        _dbContext.ServiceReactions.Remove(entity);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, "删除成功");
    }

    public async Task<ApiResponseDto> BatchDeleteAsync(List<long> ids)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("无法确定当前租户", 401);
        if (ids == null || !ids.Any())
            return ApiResponseDto.Fail("请选择要删除的数据", 400);

        var entities = await _dbContext.ServiceReactions
            .Where(p => ids.Contains(p.Id) && p.TenantId == _currentUser.TenantId.Value)
            .ToListAsync();

        _dbContext.ServiceReactions.RemoveRange(entities);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, $"成功删除 {entities.Count} 条数据");
    }
}
