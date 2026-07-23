using Mapster;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Bms.BuildingBlocks.Abstractions.Security;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Products;
using ProductCategoryEntity = Bms.Store.Domain.Entities.ProductCategory;
using Bms.Store.Infrastructure;

namespace Bms.Store.Application.Services;

/// <summary>
/// 商品分类应用服务实现
/// </summary>
public class ProductCategoryAppService : IProductCategoryAppService
{
    private readonly StoreDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<ProductCategoryCreateDto> _createValidator;
    private readonly IValidator<ProductCategoryUpdateDto> _updateValidator;

    public ProductCategoryAppService(
        StoreDbContext dbContext,
        ICurrentUser currentUser,
        IValidator<ProductCategoryCreateDto> createValidator,
        IValidator<ProductCategoryUpdateDto> updateValidator)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>
    /// 获取商品分类树
    /// </summary>
    public async Task<ApiResponseDto<List<ProductCategoryDto>>> GetTreeAsync()
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return ApiResponseDto<List<ProductCategoryDto>>.Fail("无法确定当前租户", 401);
        }

        var tenantId = _currentUser.TenantId.Value;
        var categories = await _dbContext.ProductCategories
            .Where(c => !c.IsDeleted && c.TenantId == tenantId)
            .OrderBy(c => c.Sort)
            .ThenBy(c => c.Id)
            .ToListAsync();

        var dtos = categories.Adapt<List<ProductCategoryDto>>();
        var tree = BuildTree(dtos);

        return ApiResponseDto<List<ProductCategoryDto>>.Ok(tree);
    }

    /// <summary>
    /// 创建商品分类
    /// </summary>
    public async Task<ApiResponseDto<ProductCategoryDto>> CreateAsync(ProductCategoryCreateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return ApiResponseDto<ProductCategoryDto>.Fail("无法确定当前租户", 401);
        }

        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
        {
            return ApiResponseDto<ProductCategoryDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);
        }

        var tenantId = _currentUser.TenantId.Value;
        var category = new ProductCategoryEntity
        {
            Name = dto.Name,
            ParentId = dto.ParentId == 0 ? null : dto.ParentId,
            Sort = dto.Sort,
            TenantId = tenantId,
            TenantCode = _currentUser.TenantCode ?? string.Empty,
            CreatedTime = DateTime.Now
        };

        _dbContext.ProductCategories.Add(category);
        await _dbContext.SaveChangesAsync();

        return ApiResponseDto<ProductCategoryDto>.Ok(category.Adapt<ProductCategoryDto>(), "创建成功");
    }

    /// <summary>
    /// 更新商品分类
    /// </summary>
    public async Task<ApiResponseDto<ProductCategoryDto>> UpdateAsync(ProductCategoryUpdateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return ApiResponseDto<ProductCategoryDto>.Fail("无法确定当前租户", 401);
        }

        var validation = await _updateValidator.ValidateAsync(dto);
        if (!validation.IsValid)
        {
            return ApiResponseDto<ProductCategoryDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);
        }

        var tenantId = _currentUser.TenantId.Value;
        var category = await _dbContext.ProductCategories
            .FirstOrDefaultAsync(c => c.Id == dto.Id && !c.IsDeleted && c.TenantId == tenantId);
        if (category == null)
        {
            return ApiResponseDto<ProductCategoryDto>.Fail("分类不存在", 404);
        }

        category.Name = dto.Name;
        category.ParentId = dto.ParentId == 0 ? null : dto.ParentId;
        category.Sort = dto.Sort;
        category.UpdatedTime = DateTime.Now;

        await _dbContext.SaveChangesAsync();

        return ApiResponseDto<ProductCategoryDto>.Ok(category.Adapt<ProductCategoryDto>(), "更新成功");
    }

    /// <summary>
    /// 删除商品分类（软删除）
    /// </summary>
    public async Task<ApiResponseDto> DeleteAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return ApiResponseDto.Fail("无法确定当前租户", 401);
        }

        var category = await _dbContext.ProductCategories
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted && c.TenantId == _currentUser.TenantId.Value);
        if (category == null)
        {
            return ApiResponseDto.Fail("分类不存在", 404);
        }

        // 检查是否有子分类
        var hasChildren = await _dbContext.ProductCategories
            .AnyAsync(c => c.ParentId == id && !c.IsDeleted && c.TenantId == _currentUser.TenantId.Value);
        if (hasChildren)
        {
            return ApiResponseDto.Fail("请先删除子分类", 400);
        }

        // 检查是否有关联商品
        var hasProducts = await _dbContext.Products
            .AnyAsync(p => p.CategoryId == id && !p.IsDeleted && p.TenantId == _currentUser.TenantId.Value);
        if (hasProducts)
        {
            return ApiResponseDto.Fail("该分类下有商品，无法删除", 400);
        }

        category.IsDeleted = true;
        category.UpdatedTime = DateTime.Now;
        await _dbContext.SaveChangesAsync();

        return ApiResponseDto.Success(null, "删除成功");
    }

    /// <summary>
    /// 构建分类树形结构
    /// </summary>
    private static List<ProductCategoryDto> BuildTree(List<ProductCategoryDto> categories)
    {
        var lookup = categories.ToLookup(c => c.ParentId);
        foreach (var category in categories)
        {
            category.Children = lookup[category.Id].ToList();
        }
        return categories.Where(c => c.ParentId == 0).ToList();
    }
}
