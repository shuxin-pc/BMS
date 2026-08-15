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
            return ApiResponseDto<PagedResponseDto<ServiceBomDto>>.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;

        // Join ServiceBom -> ServiceProduct -> Master（服务项目名称）
        //         + Product -> Master（耗材名称/编码/单位）
        var queryable = from bom in _dbContext.ServiceBoms
                        where !bom.IsDeleted && bom.TenantId == tenantId
                        join sp in _dbContext.ServiceProducts on bom.ServiceProductId equals sp.Id
                        join spm in _dbContext.ProductMasters on sp.MasterId equals spm.Id
                        join p in _dbContext.Products on bom.ConsumableProductId equals p.Id
                        join pm in _dbContext.ProductMasters on p.MasterId equals pm.Id
                        select new
                        {
                            bom.Id,
                            bom.ServiceProductId,
                            ServiceProductName = spm.Name,
                            bom.ConsumableProductId,
                            ConsumableProductName = pm.Name,
                            ConsumableProductCode = pm.Code,
                            Unit = pm.Unit,
                            bom.Quantity,
                            bom.CreatedTime,
                            bom.UpdatedTime
                        };

        if (query.ServiceProductId.HasValue)
            queryable = queryable.Where(x => x.ServiceProductId == query.ServiceProductId.Value);
        if (query.ConsumableProductId.HasValue)
            queryable = queryable.Where(x => x.ConsumableProductId == query.ConsumableProductId.Value);
        if (!string.IsNullOrWhiteSpace(query.ServiceProductName))
            queryable = queryable.Where(x => x.ServiceProductName.Contains(query.ServiceProductName));
        if (!string.IsNullOrWhiteSpace(query.ConsumableProductName))
            queryable = queryable.Where(x => x.ConsumableProductName.Contains(query.ConsumableProductName));

        var total = await queryable.CountAsync();
        var items = await queryable
            .OrderByDescending(x => x.CreatedTime)
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var list = items.Select(x => new ServiceBomDto
        {
            Id = x.Id,
            ServiceProductId = x.ServiceProductId,
            ServiceProductName = x.ServiceProductName,
            ConsumableProductId = x.ConsumableProductId,
            ConsumableProductName = x.ConsumableProductName,
            ConsumableProductCode = x.ConsumableProductCode,
            Unit = x.Unit,
            Quantity = x.Quantity,
            CreatedAt = x.CreatedTime,
            UpdatedAt = x.UpdatedTime
        }).ToList();

        var result = new PagedResponseDto<ServiceBomDto>
        {
            List = list,
            Total = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
        return ApiResponseDto<PagedResponseDto<ServiceBomDto>>.Ok(result);
    }

    public async Task<ApiResponseDto<ServiceBomDto?>> GetByIdAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<ServiceBomDto?>.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;

        var item = await (from bom in _dbContext.ServiceBoms
                          where !bom.IsDeleted && bom.TenantId == tenantId && bom.Id == id
                          join sp in _dbContext.ServiceProducts on bom.ServiceProductId equals sp.Id
                          join spm in _dbContext.ProductMasters on sp.MasterId equals spm.Id
                          join p in _dbContext.Products on bom.ConsumableProductId equals p.Id
                          join pm in _dbContext.ProductMasters on p.MasterId equals pm.Id
                          select new
                          {
                              bom.Id,
                              bom.ServiceProductId,
                              ServiceProductName = spm.Name,
                              bom.ConsumableProductId,
                              ConsumableProductName = pm.Name,
                              ConsumableProductCode = pm.Code,
                              Unit = pm.Unit,
                              bom.Quantity,
                              bom.CreatedTime,
                              bom.UpdatedTime
                          }).FirstOrDefaultAsync();

        if (item == null)
            return ApiResponseDto<ServiceBomDto?>.Fail("服务BOM不存在", 404);

        var dto = new ServiceBomDto
        {
            Id = item.Id,
            ServiceProductId = item.ServiceProductId,
            ServiceProductName = item.ServiceProductName,
            ConsumableProductId = item.ConsumableProductId,
            ConsumableProductName = item.ConsumableProductName,
            ConsumableProductCode = item.ConsumableProductCode,
            Unit = item.Unit,
            Quantity = item.Quantity,
            CreatedAt = item.CreatedTime,
            UpdatedAt = item.UpdatedTime
        };
        return ApiResponseDto<ServiceBomDto?>.Ok(dto);
    }

    public async Task<ApiResponseDto<ServiceBomDto>> CreateAsync(ServiceBomCreateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<ServiceBomDto>.Fail("登录状态异常，请重新登录", 401);

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
            return ApiResponseDto<ServiceBomDto>.Fail("登录状态异常，请重新登录", 401);

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
            return ApiResponseDto.Fail("登录状态异常，请重新登录", 401);

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
            return ApiResponseDto.Fail("登录状态异常，请重新登录", 401);
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

    /// <summary>
    /// 获取服务项目选项列表（用于BOM下拉选择）
    /// ServiceProduct 为租户级共享，按 TenantId 过滤；Join Master 取名称，仅返回 type=2 服务商品
    /// </summary>
    public async Task<ApiResponseDto<List<ServiceProductOptionDto>>> GetServiceProductOptionsAsync()
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<List<ServiceProductOptionDto>>.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;

        var options = await (from sp in _dbContext.ServiceProducts
                             where !sp.IsDeleted && sp.TenantId == tenantId
                             join m in _dbContext.ProductMasters on sp.MasterId equals m.Id
                             where m.Type == 2
                             select new ServiceProductOptionDto
                             {
                                 Id = sp.Id,
                                 Name = m.Name
                             })
                             .OrderBy(x => x.Name)
                             .ToListAsync();

        return ApiResponseDto<List<ServiceProductOptionDto>>.Ok(options);
    }

    /// <summary>
    /// 获取耗材商品选项列表（用于BOM下拉选择）
    /// Product 为门店档案，按 TenantId + StoreId 过滤；Join Master 取名称/编码/单位，仅返回 type=3 耗材
    /// </summary>
    public async Task<ApiResponseDto<List<ConsumableOptionDto>>> GetConsumableOptionsAsync()
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<List<ConsumableOptionDto>>.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId ?? 0;

        var options = await (from p in _dbContext.Products
                             where !p.IsDeleted && p.TenantId == tenantId && p.StoreId == storeId
                             join m in _dbContext.ProductMasters on p.MasterId equals m.Id
                             where m.Type == 3
                             select new ConsumableOptionDto
                             {
                                 Id = p.Id,
                                 Name = m.Name,
                                 Code = m.Code,
                                 Unit = m.Unit
                             })
                             .OrderBy(x => x.Name)
                             .ToListAsync();

        return ApiResponseDto<List<ConsumableOptionDto>>.Ok(options);
    }
}
