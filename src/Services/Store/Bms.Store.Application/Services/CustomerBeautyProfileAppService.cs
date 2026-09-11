using Mapster;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Bms.BuildingBlocks.Abstractions.Security;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Customers;
using CustomerBeautyProfileEntity = Bms.Store.Domain.Entities.CustomerBeautyProfile;
using Bms.Store.Infrastructure;

namespace Bms.Store.Application.Services;

/// <summary>
/// 客户美容档案应用服务实现
/// </summary>
public class CustomerBeautyProfileAppService : ICustomerBeautyProfileAppService
{
    private readonly StoreDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<CustomerBeautyProfileCreateDto> _createValidator;
    private readonly IValidator<CustomerBeautyProfileUpdateDto> _updateValidator;

    public CustomerBeautyProfileAppService(
        StoreDbContext dbContext,
        ICurrentUser currentUser,
        IValidator<CustomerBeautyProfileCreateDto> createValidator,
        IValidator<CustomerBeautyProfileUpdateDto> updateValidator)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>
    /// 获取客户美容档案分页列表
    /// 支持按客户姓名、手机号模糊搜索和肤质类型过滤
    /// </summary>
    public async Task<ApiResponseDto<PagedResponseDto<CustomerBeautyProfileDto>>> GetPagedListAsync(CustomerBeautyProfileQueryDto query)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<PagedResponseDto<CustomerBeautyProfileDto>>.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId ?? 0;
        var queryable = from p in _dbContext.CustomerBeautyProfiles
                        join c in _dbContext.Customers on p.CustomerId equals c.Id
                        where !p.IsDeleted && p.TenantId == tenantId && p.StoreId == storeId && !c.IsDeleted
                        select new { p, c };

        if (query.CustomerId.HasValue)
            queryable = queryable.Where(x => x.p.CustomerId == query.CustomerId.Value);
        // 客户名称/手机号合并关键字查询：命中姓名或手机号其一即满足（对齐预约列表）
        if (!string.IsNullOrWhiteSpace(query.Keyword))
            queryable = queryable.Where(x => x.c.Name.Contains(query.Keyword) || x.c.Phone.Contains(query.Keyword));
        if (!string.IsNullOrWhiteSpace(query.SkinType))
            queryable = queryable.Where(x => x.p.SkinType == query.SkinType);

        var total = await queryable.CountAsync();
        var items = await queryable
            .OrderByDescending(x => x.p.CreatedTime)
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(x => new CustomerBeautyProfileDto
            {
                Id = x.p.Id,
                CustomerId = x.p.CustomerId,
                CustomerName = x.c.Name,
                CustomerPhone = x.c.Phone,
                SkinType = x.p.SkinType,
                Sensitivity = x.p.Sensitivity,
                HairType = x.p.HairType,
                AllergyHistory = x.p.AllergyHistory,
                Remark = x.p.Remark,
                CreatedAt = x.p.CreatedTime,
                UpdatedAt = x.p.UpdatedTime
            })
            .ToListAsync();

        var result = new PagedResponseDto<CustomerBeautyProfileDto>
        {
            List = items,
            Total = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
        return ApiResponseDto<PagedResponseDto<CustomerBeautyProfileDto>>.Ok(result);
    }

    /// <summary>
    /// 根据ID获取客户美容档案详情
    /// </summary>
    public async Task<ApiResponseDto<CustomerBeautyProfileDto?>> GetByIdAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<CustomerBeautyProfileDto?>.Fail("登录状态异常，请重新登录", 401);

        var dto = await GetDtoByIdAsync(id);
        if (dto == null)
            return ApiResponseDto<CustomerBeautyProfileDto?>.Fail("客户美容档案不存在", 404);
        return ApiResponseDto<CustomerBeautyProfileDto?>.Ok(dto);
    }

    /// <summary>
    /// 创建客户美容档案
    /// </summary>
    public async Task<ApiResponseDto<CustomerBeautyProfileDto>> CreateAsync(CustomerBeautyProfileCreateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<CustomerBeautyProfileDto>.Fail("登录状态异常，请重新登录", 401);

        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<CustomerBeautyProfileDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId ?? 0;

        // 唯一性校验：同租户同门店下每个客户仅允许一份未删除的美容档案
        var exists = await _dbContext.CustomerBeautyProfiles
            .AnyAsync(p => !p.IsDeleted && p.TenantId == tenantId && p.StoreId == storeId && p.CustomerId == dto.CustomerId);
        if (exists)
            return ApiResponseDto<CustomerBeautyProfileDto>.Fail("该客户已存在美容档案", 400);

        var entity = dto.Adapt<CustomerBeautyProfileEntity>();
        entity.TenantId = tenantId;
        entity.TenantCode = _currentUser.TenantCode ?? string.Empty;
        entity.StoreId = storeId;
        entity.StoreCode = _currentUser.StoreCode ?? string.Empty;
        entity.CreatedTime = DateTime.Now;

        _dbContext.CustomerBeautyProfiles.Add(entity);
        await _dbContext.SaveChangesAsync();

        // 保存后重新查询以填充客户姓名和手机号
        var result = await GetDtoByIdAsync(entity.Id);
        return ApiResponseDto<CustomerBeautyProfileDto>.Ok(result!, "创建成功");
    }

    /// <summary>
    /// 更新客户美容档案
    /// </summary>
    public async Task<ApiResponseDto<CustomerBeautyProfileDto>> UpdateAsync(CustomerBeautyProfileUpdateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<CustomerBeautyProfileDto>.Fail("登录状态异常，请重新登录", 401);

        var validation = await _updateValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<CustomerBeautyProfileDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId ?? 0;
        var entity = await _dbContext.CustomerBeautyProfiles
            .FirstOrDefaultAsync(p => p.Id == dto.Id && !p.IsDeleted && p.TenantId == tenantId && p.StoreId == storeId);
        if (entity == null)
            return ApiResponseDto<CustomerBeautyProfileDto>.Fail("客户美容档案不存在", 404);

        entity.CustomerId = dto.CustomerId;
        entity.SkinType = dto.SkinType;
        entity.Sensitivity = dto.Sensitivity;
        entity.HairType = dto.HairType;
        entity.AllergyHistory = dto.AllergyHistory;
        entity.Remark = dto.Remark;
        entity.UpdatedTime = DateTime.Now;

        await _dbContext.SaveChangesAsync();

        // 保存后重新查询以填充客户姓名和手机号
        var result = await GetDtoByIdAsync(entity.Id);
        return ApiResponseDto<CustomerBeautyProfileDto>.Ok(result!, "更新成功");
    }

    /// <summary>
    /// 删除客户美容档案（软删除）
    /// </summary>
    public async Task<ApiResponseDto> DeleteAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId ?? 0;
        var entity = await _dbContext.CustomerBeautyProfiles
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted && p.TenantId == tenantId && p.StoreId == storeId);
        if (entity == null)
            return ApiResponseDto.Fail("客户美容档案不存在", 404);

        entity.IsDeleted = true;
        entity.UpdatedTime = DateTime.Now;
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, "删除成功");
    }

    /// <summary>
    /// 批量删除客户美容档案（软删除）
    /// </summary>
    public async Task<ApiResponseDto> BatchDeleteAsync(List<long> ids)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("登录状态异常，请重新登录", 401);
        if (ids == null || !ids.Any())
            return ApiResponseDto.Fail("请选择要删除的数据", 400);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId ?? 0;
        var entities = await _dbContext.CustomerBeautyProfiles
            .Where(p => ids.Contains(p.Id) && !p.IsDeleted && p.TenantId == tenantId && p.StoreId == storeId)
            .ToListAsync();

        foreach (var entity in entities)
        {
            entity.IsDeleted = true;
            entity.UpdatedTime = DateTime.Now;
        }
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, $"成功删除 {entities.Count} 条数据");
    }

    /// <summary>
    /// 通过 JOIN Customer 查询完整 DTO（含客户姓名和手机号）
    /// </summary>
    private async Task<CustomerBeautyProfileDto?> GetDtoByIdAsync(long id)
    {
        var tenantId = _currentUser.TenantId!.Value;
        var storeId = _currentUser.StoreId ?? 0;
        return await (from p in _dbContext.CustomerBeautyProfiles
                      join c in _dbContext.Customers on p.CustomerId equals c.Id
                      where p.Id == id && !p.IsDeleted && p.TenantId == tenantId && p.StoreId == storeId && !c.IsDeleted
                      select new CustomerBeautyProfileDto
                      {
                          Id = p.Id,
                          CustomerId = p.CustomerId,
                          CustomerName = c.Name,
                          CustomerPhone = c.Phone,
                          SkinType = p.SkinType,
                          Sensitivity = p.Sensitivity,
                          HairType = p.HairType,
                          AllergyHistory = p.AllergyHistory,
                          Remark = p.Remark,
                          CreatedAt = p.CreatedTime,
                          UpdatedAt = p.UpdatedTime
                      }).FirstOrDefaultAsync();
    }
}
