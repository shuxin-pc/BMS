using Mapster;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Bms.BuildingBlocks.Abstractions.Security;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Statistics;
using ProductSalesStatEntity = Bms.Store.Domain.Entities.ProductSalesStat;
using Bms.Store.Infrastructure;

namespace Bms.Store.Application.Services;

/// <summary>
/// 商品销售统计应用服务实现
/// </summary>
public class ProductSalesStatAppService : IProductSalesStatAppService
{
    private readonly StoreDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<ProductSalesStatCreateDto> _createValidator;
    private readonly IValidator<ProductSalesStatUpdateDto> _updateValidator;

    public ProductSalesStatAppService(
        StoreDbContext dbContext,
        ICurrentUser currentUser,
        IValidator<ProductSalesStatCreateDto> createValidator,
        IValidator<ProductSalesStatUpdateDto> updateValidator)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>
    /// 获取商品销售统计分页列表
    /// </summary>
    public async Task<ApiResponseDto<PagedResponseDto<ProductSalesStatDto>>> GetPagedListAsync(ProductSalesStatQueryDto query)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<PagedResponseDto<ProductSalesStatDto>>.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;
        var queryable = _dbContext.ProductSalesStats
            .Where(s => s.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(query.StatMonth))
            queryable = queryable.Where(s => s.StatMonth == query.StatMonth);
        if (query.ProductId.HasValue)
            queryable = queryable.Where(s => s.ProductId == query.ProductId.Value);
        if (query.ProductType.HasValue)
            queryable = queryable.Where(s => s.ProductType == query.ProductType.Value);

        var total = await queryable.CountAsync();
        var items = await queryable
            .OrderByDescending(s => s.StatDate)
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var result = new PagedResponseDto<ProductSalesStatDto>
        {
            List = items.Adapt<List<ProductSalesStatDto>>(),
            Total = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
        return ApiResponseDto<PagedResponseDto<ProductSalesStatDto>>.Ok(result);
    }

    /// <summary>
    /// 根据ID获取商品销售统计详情
    /// </summary>
    public async Task<ApiResponseDto<ProductSalesStatDto?>> GetByIdAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<ProductSalesStatDto?>.Fail("登录状态异常，请重新登录", 401);

        var entity = await _dbContext.ProductSalesStats
            .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == _currentUser.TenantId.Value);
        if (entity == null)
            return ApiResponseDto<ProductSalesStatDto?>.Fail("商品销售统计不存在", 404);
        return ApiResponseDto<ProductSalesStatDto?>.Ok(entity.Adapt<ProductSalesStatDto>());
    }

    /// <summary>
    /// 创建商品销售统计
    /// </summary>
    public async Task<ApiResponseDto<ProductSalesStatDto>> CreateAsync(ProductSalesStatCreateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<ProductSalesStatDto>.Fail("登录状态异常，请重新登录", 401);

        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<ProductSalesStatDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var entity = dto.Adapt<ProductSalesStatEntity>();
        entity.TenantId = tenantId;
        entity.TenantCode = _currentUser.TenantCode ?? string.Empty;
        entity.CreatedTime = DateTime.Now;

        _dbContext.ProductSalesStats.Add(entity);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto<ProductSalesStatDto>.Ok(entity.Adapt<ProductSalesStatDto>(), "创建成功");
    }

    /// <summary>
    /// 更新商品销售统计
    /// </summary>
    public async Task<ApiResponseDto<ProductSalesStatDto>> UpdateAsync(ProductSalesStatUpdateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<ProductSalesStatDto>.Fail("登录状态异常，请重新登录", 401);

        var validation = await _updateValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<ProductSalesStatDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var entity = await _dbContext.ProductSalesStats
            .FirstOrDefaultAsync(s => s.Id == dto.Id && s.TenantId == tenantId);
        if (entity == null)
            return ApiResponseDto<ProductSalesStatDto>.Fail("商品销售统计不存在", 404);

        entity.StatDate = dto.StatDate;
        entity.StatMonth = dto.StatMonth;
        entity.ProductId = dto.ProductId;
        entity.ProductName = dto.ProductName;
        entity.ProductType = dto.ProductType;
        entity.SalesCount = dto.SalesCount;
        entity.SalesAmount = dto.SalesAmount;
        entity.UpdatedTime = DateTime.Now;

        await _dbContext.SaveChangesAsync();
        return ApiResponseDto<ProductSalesStatDto>.Ok(entity.Adapt<ProductSalesStatDto>(), "更新成功");
    }

    /// <summary>
    /// 删除商品销售统计（物理删除）
    /// </summary>
    public async Task<ApiResponseDto> DeleteAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("登录状态异常，请重新登录", 401);

        var entity = await _dbContext.ProductSalesStats
            .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == _currentUser.TenantId.Value);
        if (entity == null)
            return ApiResponseDto.Fail("商品销售统计不存在", 404);

        _dbContext.ProductSalesStats.Remove(entity);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, "删除成功");
    }

    /// <summary>
    /// 批量删除商品销售统计（物理删除）
    /// </summary>
    public async Task<ApiResponseDto> BatchDeleteAsync(List<long> ids)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("登录状态异常，请重新登录", 401);
        if (ids == null || !ids.Any())
            return ApiResponseDto.Fail("请选择要删除的数据", 400);

        var entities = await _dbContext.ProductSalesStats
            .Where(s => ids.Contains(s.Id) && s.TenantId == _currentUser.TenantId.Value)
            .ToListAsync();

        _dbContext.ProductSalesStats.RemoveRange(entities);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, $"成功删除 {entities.Count} 条数据");
    }
}
