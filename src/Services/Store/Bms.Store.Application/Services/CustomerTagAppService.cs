using Mapster;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Bms.BuildingBlocks.Abstractions.Security;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Customers;
using Bms.Store.Domain.Entities;
using CustomerTagEntity = Bms.Store.Domain.Entities.CustomerTag;
using Bms.Store.Infrastructure;

namespace Bms.Store.Application.Services;

/// <summary>
/// 客户标签应用服务实现
/// 标签字典按门店隔离，同门店内标签名称唯一
/// </summary>
public class CustomerTagAppService : ICustomerTagAppService
{
    private readonly StoreDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<CustomerTagCreateDto> _createValidator;
    private readonly IValidator<CustomerTagUpdateDto> _updateValidator;

    public CustomerTagAppService(
        StoreDbContext dbContext,
        ICurrentUser currentUser,
        IValidator<CustomerTagCreateDto> createValidator,
        IValidator<CustomerTagUpdateDto> updateValidator)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>
    /// 获取客户标签分页列表
    /// </summary>
    public async Task<ApiResponseDto<PagedResponseDto<CustomerTagDto>>> GetPagedListAsync(CustomerTagQueryDto query)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<PagedResponseDto<CustomerTagDto>>.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId ?? 0;
        var queryable = _dbContext.CustomerTags
            .Where(t => !t.IsDeleted && t.TenantId == tenantId && t.StoreId == storeId);

        if (!string.IsNullOrWhiteSpace(query.Name))
            queryable = queryable.Where(t => t.Name.Contains(query.Name));

        var total = await queryable.CountAsync();
        var items = await queryable
            .OrderBy(t => t.Sort).ThenByDescending(t => t.CreatedTime)
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var result = new PagedResponseDto<CustomerTagDto>
        {
            List = items.Adapt<List<CustomerTagDto>>(),
            Total = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
        return ApiResponseDto<PagedResponseDto<CustomerTagDto>>.Ok(result);
    }

    /// <summary>
    /// 获取全量客户标签列表（供客户弹窗下拉使用，不分页）
    /// </summary>
    public async Task<ApiResponseDto<List<CustomerTagDto>>> GetAllAsync()
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<List<CustomerTagDto>>.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId ?? 0;
        var items = await _dbContext.CustomerTags
            .Where(t => !t.IsDeleted && t.TenantId == tenantId && t.StoreId == storeId)
            .OrderBy(t => t.Sort).ThenByDescending(t => t.CreatedTime)
            .ToListAsync();

        return ApiResponseDto<List<CustomerTagDto>>.Ok(items.Adapt<List<CustomerTagDto>>());
    }

    /// <summary>
    /// 根据ID获取客户标签详情
    /// </summary>
    public async Task<ApiResponseDto<CustomerTagDto?>> GetByIdAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<CustomerTagDto?>.Fail("登录状态异常，请重新登录", 401);

        var entity = await _dbContext.CustomerTags
            .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted
                && t.TenantId == _currentUser.TenantId.Value
                && t.StoreId == (_currentUser.StoreId ?? 0));
        if (entity == null)
            return ApiResponseDto<CustomerTagDto?>.Fail("客户标签不存在", 404);
        return ApiResponseDto<CustomerTagDto?>.Ok(entity.Adapt<CustomerTagDto>());
    }

    /// <summary>
    /// 创建客户标签
    /// 校验：同门店内 Name 不重复
    /// </summary>
    public async Task<ApiResponseDto<CustomerTagDto>> CreateAsync(CustomerTagCreateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<CustomerTagDto>.Fail("登录状态异常，请重新登录", 401);

        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<CustomerTagDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId ?? 0;

        // 校验同门店内名称不重复（唯一索引兜底，应用层提前校验给出友好提示）
        var nameExists = await _dbContext.CustomerTags
            .AnyAsync(t => !t.IsDeleted && t.TenantId == tenantId && t.StoreId == storeId && t.Name == dto.Name);
        if (nameExists)
            return ApiResponseDto<CustomerTagDto>.Fail($"标签名称 {dto.Name} 已存在", 400);

        var entity = dto.Adapt<CustomerTagEntity>();
        entity.TenantId = tenantId;
        entity.TenantCode = _currentUser.TenantCode ?? string.Empty;
        entity.StoreId = storeId;
        entity.StoreCode = _currentUser.StoreCode ?? string.Empty;
        entity.CreatedTime = DateTime.Now;

        _dbContext.CustomerTags.Add(entity);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto<CustomerTagDto>.Ok(entity.Adapt<CustomerTagDto>(), "创建成功");
    }

    /// <summary>
    /// 更新客户标签
    /// 允许修改 Name，校验同门店内 Name 不重复（排除自身）
    /// </summary>
    public async Task<ApiResponseDto<CustomerTagDto>> UpdateAsync(CustomerTagUpdateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<CustomerTagDto>.Fail("登录状态异常，请重新登录", 401);

        var validation = await _updateValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<CustomerTagDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId ?? 0;
        var entity = await _dbContext.CustomerTags
            .FirstOrDefaultAsync(t => t.Id == dto.Id && !t.IsDeleted
                && t.TenantId == tenantId && t.StoreId == storeId);
        if (entity == null)
            return ApiResponseDto<CustomerTagDto>.Fail("客户标签不存在", 404);

        if (entity.Name != dto.Name)
        {
            var nameExists = await _dbContext.CustomerTags
                .AnyAsync(t => !t.IsDeleted && t.TenantId == tenantId && t.StoreId == storeId
                    && t.Name == dto.Name && t.Id != dto.Id);
            if (nameExists)
                return ApiResponseDto<CustomerTagDto>.Fail($"标签名称 {dto.Name} 已存在", 400);
        }

        entity.Name = dto.Name;
        entity.Color = dto.Color;
        entity.Sort = dto.Sort;
        entity.Remark = dto.Remark;
        entity.UpdatedTime = DateTime.Now;

        await _dbContext.SaveChangesAsync();
        return ApiResponseDto<CustomerTagDto>.Ok(entity.Adapt<CustomerTagDto>(), "更新成功");
    }

    /// <summary>
    /// 删除客户标签（软删除）
    /// 校验：有关联客户的标签不可删除
    /// </summary>
    public async Task<ApiResponseDto> DeleteAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("登录状态异常，请重新登录", 401);

        var entity = await _dbContext.CustomerTags
            .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted
                && t.TenantId == _currentUser.TenantId.Value
                && t.StoreId == (_currentUser.StoreId ?? 0));
        if (entity == null)
            return ApiResponseDto.Fail("客户标签不存在", 404);

        // 有关联客户的标签不可删除
        var hasCustomers = await _dbContext.CustomerTagLinks
            .AnyAsync(l => l.TagId == id && !l.IsDeleted
                && l.TenantId == _currentUser.TenantId.Value
                && l.StoreId == (_currentUser.StoreId ?? 0));
        if (hasCustomers)
            return ApiResponseDto.Fail("该标签下存在关联客户，不可删除", 400);

        entity.IsDeleted = true;
        entity.UpdatedTime = DateTime.Now;
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, "删除成功");
    }

    /// <summary>
    /// 批量删除客户标签（软删除）
    /// 校验：有关联客户的标签不可删除
    /// </summary>
    public async Task<ApiResponseDto> BatchDeleteAsync(List<long> ids)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("登录状态异常，请重新登录", 401);
        if (ids == null || !ids.Any())
            return ApiResponseDto.Fail("请选择要删除的数据", 400);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId ?? 0;
        var entities = await _dbContext.CustomerTags
            .Where(t => ids.Contains(t.Id) && !t.IsDeleted && t.TenantId == tenantId && t.StoreId == storeId)
            .ToListAsync();

        // 校验关联客户
        var tagIds = entities.Select(e => e.Id).ToList();
        var hasCustomers = await _dbContext.CustomerTagLinks
            .AnyAsync(l => !l.IsDeleted && tagIds.Contains(l.TagId)
                && l.TenantId == tenantId && l.StoreId == storeId);
        if (hasCustomers)
            return ApiResponseDto.Fail("所选标签中存在关联客户的标签，不可删除", 400);

        foreach (var entity in entities)
        {
            entity.IsDeleted = true;
            entity.UpdatedTime = DateTime.Now;
        }
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, $"成功删除 {entities.Count} 条数据");
    }
}
