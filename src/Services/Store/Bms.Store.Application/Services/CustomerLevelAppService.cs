using Mapster;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Bms.BuildingBlocks.Abstractions.Security;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Customers;
using Bms.Store.Domain.Constants;
using CustomerLevelEntity = Bms.Store.Domain.Entities.CustomerLevel;
using Bms.Store.Infrastructure;

namespace Bms.Store.Application.Services;

/// <summary>
/// 客户等级应用服务实现
/// 等级数量与等级值由门店自由管理，同租户内 Level、Code 唯一
/// </summary>
public class CustomerLevelAppService : ICustomerLevelAppService
{
    private readonly StoreDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<CustomerLevelCreateDto> _createValidator;
    private readonly IValidator<CustomerLevelUpdateDto> _updateValidator;

    public CustomerLevelAppService(
        StoreDbContext dbContext,
        ICurrentUser currentUser,
        IValidator<CustomerLevelCreateDto> createValidator,
        IValidator<CustomerLevelUpdateDto> updateValidator)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>
    /// 获取客户等级分页列表
    /// 若当前租户尚无任何客户等级，则自动 Seed 两个默认等级（普通会员、会员）
    /// </summary>
    public async Task<ApiResponseDto<PagedResponseDto<CustomerLevelDto>>> GetPagedListAsync(CustomerLevelQueryDto query)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<PagedResponseDto<CustomerLevelDto>>.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;
        await EnsureDefaultLevelsAsync(tenantId);

        var queryable = _dbContext.CustomerLevels
            .Where(c => !c.IsDeleted && c.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(query.Name))
            queryable = queryable.Where(c => c.Name.Contains(query.Name));
        if (!string.IsNullOrWhiteSpace(query.Code))
            queryable = queryable.Where(c => c.Code.Contains(query.Code));

        var total = await queryable.CountAsync();
        var items = await queryable
            .OrderByDescending(c => c.CreatedTime)
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var result = new PagedResponseDto<CustomerLevelDto>
        {
            List = items.Adapt<List<CustomerLevelDto>>(),
            Total = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
        return ApiResponseDto<PagedResponseDto<CustomerLevelDto>>.Ok(result);
    }

    /// <summary>
    /// 根据ID获取客户等级详情
    /// </summary>
    public async Task<ApiResponseDto<CustomerLevelDto?>> GetByIdAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<CustomerLevelDto?>.Fail("登录状态异常，请重新登录", 401);

        var entity = await _dbContext.CustomerLevels
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted && c.TenantId == _currentUser.TenantId.Value);
        if (entity == null)
            return ApiResponseDto<CustomerLevelDto?>.Fail("客户等级不存在", 404);
        return ApiResponseDto<CustomerLevelDto?>.Ok(entity.Adapt<CustomerLevelDto>());
    }

    /// <summary>
    /// 创建客户等级
    /// 校验：同租户内 Level、Code 不重复
    /// </summary>
    public async Task<ApiResponseDto<CustomerLevelDto>> CreateAsync(CustomerLevelCreateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<CustomerLevelDto>.Fail("登录状态异常，请重新登录", 401);

        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<CustomerLevelDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;

        // 校验同一 Level 不重复（唯一索引兜底，应用层提前校验给出友好提示）
        var levelExists = await _dbContext.CustomerLevels
            .AnyAsync(c => !c.IsDeleted && c.TenantId == tenantId && c.Level == dto.Level);
        if (levelExists)
            return ApiResponseDto<CustomerLevelDto>.Fail($"等级值 {dto.Level} 已存在", 400);

        var codeExists = await _dbContext.CustomerLevels
            .AnyAsync(c => c.Code == dto.Code && c.TenantId == tenantId && !c.IsDeleted);
        if (codeExists)
            return ApiResponseDto<CustomerLevelDto>.Fail($"等级编码 {dto.Code} 已存在", 400);

        var entity = dto.Adapt<CustomerLevelEntity>();
        entity.TenantId = tenantId;
        entity.TenantCode = _currentUser.TenantCode ?? string.Empty;
        entity.CreatedTime = DateTime.Now;

        _dbContext.CustomerLevels.Add(entity);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto<CustomerLevelDto>.Ok(entity.Adapt<CustomerLevelDto>(), "创建成功");
    }

    /// <summary>
    /// 更新客户等级
    /// 允许修改 Level，校验同租户内 Level、Code 不重复（排除自身）
    /// </summary>
    public async Task<ApiResponseDto<CustomerLevelDto>> UpdateAsync(CustomerLevelUpdateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<CustomerLevelDto>.Fail("登录状态异常，请重新登录", 401);

        var validation = await _updateValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<CustomerLevelDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var entity = await _dbContext.CustomerLevels
            .FirstOrDefaultAsync(c => c.Id == dto.Id && !c.IsDeleted && c.TenantId == tenantId);
        if (entity == null)
            return ApiResponseDto<CustomerLevelDto>.Fail("客户等级不存在", 404);

        // Level 变更时校验同租户内不重复
        if (entity.Level != dto.Level)
        {
            var levelExists = await _dbContext.CustomerLevels
                .AnyAsync(c => c.Level == dto.Level && c.TenantId == tenantId && !c.IsDeleted && c.Id != dto.Id);
            if (levelExists)
                return ApiResponseDto<CustomerLevelDto>.Fail($"等级值 {dto.Level} 已存在", 400);
        }

        if (entity.Code != dto.Code)
        {
            var codeExists = await _dbContext.CustomerLevels
                .AnyAsync(c => c.Code == dto.Code && c.TenantId == tenantId && !c.IsDeleted && c.Id != dto.Id);
            if (codeExists)
                return ApiResponseDto<CustomerLevelDto>.Fail($"等级编码 {dto.Code} 已存在", 400);
        }

        entity.Name = dto.Name;
        entity.Code = dto.Code;
        entity.Level = dto.Level;
        entity.DiscountRate = dto.DiscountRate;
        entity.Remark = dto.Remark;
        entity.UpdatedTime = DateTime.Now;

        await _dbContext.SaveChangesAsync();
        return ApiResponseDto<CustomerLevelDto>.Ok(entity.Adapt<CustomerLevelDto>(), "更新成功");
    }

    /// <summary>
    /// 删除客户等级（软删除）
    /// 校验：有关联客户的等级不可删除
    /// </summary>
    public async Task<ApiResponseDto> DeleteAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("登录状态异常，请重新登录", 401);

        var entity = await _dbContext.CustomerLevels
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted && c.TenantId == _currentUser.TenantId.Value);
        if (entity == null)
            return ApiResponseDto.Fail("客户等级不存在", 404);

        // 有关联客户的等级不可删除
        var hasCustomers = await _dbContext.Customers
            .AnyAsync(c => c.LevelId == id && !c.IsDeleted && c.TenantId == _currentUser.TenantId.Value);
        if (hasCustomers)
            return ApiResponseDto.Fail("该等级下存在关联客户，不可删除", 400);

        entity.IsDeleted = true;
        entity.UpdatedTime = DateTime.Now;
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, "删除成功");
    }

    /// <summary>
    /// 批量删除客户等级（软删除）
    /// 校验：有关联客户的等级不可删除
    /// </summary>
    public async Task<ApiResponseDto> BatchDeleteAsync(List<long> ids)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("登录状态异常，请重新登录", 401);
        if (ids == null || !ids.Any())
            return ApiResponseDto.Fail("请选择要删除的数据", 400);

        var tenantId = _currentUser.TenantId.Value;
        var entities = await _dbContext.CustomerLevels
            .Where(c => ids.Contains(c.Id) && !c.IsDeleted && c.TenantId == tenantId)
            .ToListAsync();

        // 校验关联客户
        var levelIds = entities.Select(e => e.Id).ToList();
        var hasCustomers = await _dbContext.Customers
            .AnyAsync(c => c.LevelId.HasValue && levelIds.Contains(c.LevelId.Value) && !c.IsDeleted && c.TenantId == tenantId);
        if (hasCustomers)
            return ApiResponseDto.Fail("所选等级中存在关联客户的等级，不可删除", 400);

        foreach (var entity in entities)
        {
            entity.IsDeleted = true;
            entity.UpdatedTime = DateTime.Now;
        }
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, $"成功删除 {entities.Count} 条数据");
    }

    /// <summary>
    /// 确保当前租户存在默认的两个客户等级（普通会员、会员）
    /// 若租户已存在等级（包括软删除的历史等级）则跳过；否则自动创建两个默认等级
    /// 采用懒初始化模式：新租户首次查询客户等级时自动 Seed，无需跨服务事件订阅
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    private async Task EnsureDefaultLevelsAsync(long tenantId)
    {
        // 已存在未删除等级则跳过（含软删除历史等级也视为已初始化过）
        var anyExists = await _dbContext.CustomerLevels
            .AnyAsync(c => c.TenantId == tenantId);
        if (anyExists)
            return;

        var tenantCode = _currentUser.TenantCode ?? string.Empty;
        var now = DateTime.Now;

        // 普通会员（默认等级）
        var normal = new CustomerLevelEntity
        {
            Name = "普通会员",
            Code = "NORMAL",
            Level = CustomerLevelTypes.Normal,
            DiscountRate = 1.0m,
            TenantId = tenantId,
            TenantCode = tenantCode,
            CreatedTime = now
        };

        // 会员（升级等级）
        var member = new CustomerLevelEntity
        {
            Name = "会员",
            Code = "MEMBER",
            Level = CustomerLevelTypes.Member,
            DiscountRate = 0.9m,
            TenantId = tenantId,
            TenantCode = tenantCode,
            CreatedTime = now
        };

        _dbContext.CustomerLevels.AddRange(normal, member);
        await _dbContext.SaveChangesAsync();
    }
}
