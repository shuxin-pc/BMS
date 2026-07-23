using Mapster;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Bms.BuildingBlocks.Abstractions.Security;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Customers;
using ConsumeLogEntity = Bms.Store.Domain.Entities.ConsumeLog;
using Bms.Store.Infrastructure;

namespace Bms.Store.Application.Services;

/// <summary>
/// 消费记录应用服务实现
/// </summary>
public class ConsumeLogAppService : IConsumeLogAppService
{
    private readonly StoreDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<ConsumeLogCreateDto> _createValidator;
    private readonly IValidator<ConsumeLogUpdateDto> _updateValidator;

    public ConsumeLogAppService(
        StoreDbContext dbContext,
        ICurrentUser currentUser,
        IValidator<ConsumeLogCreateDto> createValidator,
        IValidator<ConsumeLogUpdateDto> updateValidator)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>
    /// 获取消费记录分页列表
    /// </summary>
    public async Task<ApiResponseDto<PagedResponseDto<ConsumeLogDto>>> GetPagedListAsync(ConsumeLogQueryDto query)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<PagedResponseDto<ConsumeLogDto>>.Fail("无法确定当前租户", 401);

        var tenantId = _currentUser.TenantId.Value;
        var queryable = _dbContext.ConsumeLogs
            .Where(p => p.TenantId == tenantId);

        if (query.CustomerId.HasValue)
            queryable = queryable.Where(p => p.CustomerId == query.CustomerId.Value);
        if (query.OrderId.HasValue)
            queryable = queryable.Where(p => p.OrderId == query.OrderId.Value);

        var total = await queryable.CountAsync();
        var items = await queryable
            .OrderByDescending(p => p.CreatedTime)
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var result = new PagedResponseDto<ConsumeLogDto>
        {
            List = items.Adapt<List<ConsumeLogDto>>(),
            Total = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
        return ApiResponseDto<PagedResponseDto<ConsumeLogDto>>.Ok(result);
    }

    /// <summary>
    /// 根据ID获取消费记录详情
    /// </summary>
    public async Task<ApiResponseDto<ConsumeLogDto?>> GetByIdAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<ConsumeLogDto?>.Fail("无法确定当前租户", 401);

        var entity = await _dbContext.ConsumeLogs
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == _currentUser.TenantId.Value);
        if (entity == null)
            return ApiResponseDto<ConsumeLogDto?>.Fail("消费记录不存在", 404);
        return ApiResponseDto<ConsumeLogDto?>.Ok(entity.Adapt<ConsumeLogDto>());
    }

    /// <summary>
    /// 创建消费记录
    /// </summary>
    public async Task<ApiResponseDto<ConsumeLogDto>> CreateAsync(ConsumeLogCreateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<ConsumeLogDto>.Fail("无法确定当前租户", 401);

        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<ConsumeLogDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var entity = dto.Adapt<ConsumeLogEntity>();
        entity.TenantId = tenantId;
        entity.TenantCode = _currentUser.TenantCode ?? string.Empty;
        entity.CreatedTime = DateTime.Now;

        _dbContext.ConsumeLogs.Add(entity);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto<ConsumeLogDto>.Ok(entity.Adapt<ConsumeLogDto>(), "创建成功");
    }

    /// <summary>
    /// 更新消费记录
    /// </summary>
    public async Task<ApiResponseDto<ConsumeLogDto>> UpdateAsync(ConsumeLogUpdateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<ConsumeLogDto>.Fail("无法确定当前租户", 401);

        var validation = await _updateValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<ConsumeLogDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var entity = await _dbContext.ConsumeLogs
            .FirstOrDefaultAsync(p => p.Id == dto.Id && p.TenantId == tenantId);
        if (entity == null)
            return ApiResponseDto<ConsumeLogDto>.Fail("消费记录不存在", 404);

        entity.CustomerId = dto.CustomerId;
        entity.OrderId = dto.OrderId;
        entity.Amount = dto.Amount;
        entity.Points = dto.Points;
        entity.ConsumeTime = dto.ConsumeTime;
        entity.Remark = dto.Remark;
        entity.UpdatedTime = DateTime.Now;

        await _dbContext.SaveChangesAsync();
        return ApiResponseDto<ConsumeLogDto>.Ok(entity.Adapt<ConsumeLogDto>(), "更新成功");
    }

    /// <summary>
    /// 删除消费记录（软删除）
    /// </summary>
    public async Task<ApiResponseDto> DeleteAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("无法确定当前租户", 401);

        var entity = await _dbContext.ConsumeLogs
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == _currentUser.TenantId.Value);
        if (entity == null)
            return ApiResponseDto.Fail("消费记录不存在", 404);

        _dbContext.ConsumeLogs.Remove(entity);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, "删除成功");
    }

    /// <summary>
    /// 批量删除消费记录（软删除）
    /// </summary>
    public async Task<ApiResponseDto> BatchDeleteAsync(List<long> ids)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("无法确定当前租户", 401);
        if (ids == null || !ids.Any())
            return ApiResponseDto.Fail("请选择要删除的数据", 400);

        var entities = await _dbContext.ConsumeLogs
            .Where(p => ids.Contains(p.Id) && p.TenantId == _currentUser.TenantId.Value)
            .ToListAsync();

        _dbContext.ConsumeLogs.RemoveRange(entities);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, $"成功删除 {entities.Count} 条数据");
    }
}
