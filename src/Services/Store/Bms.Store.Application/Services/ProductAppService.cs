using Mapster;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Bms.BuildingBlocks.Abstractions.Security;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Products;
using Bms.Store.Application.Dtos.Suppliers;
using ProductEntity = Bms.Store.Domain.Entities.Product;
using ServiceProductEntity = Bms.Store.Domain.Entities.ServiceProduct;
using Bms.Store.Domain.Entities;
using Bms.Store.Infrastructure;

namespace Bms.Store.Application.Services;

/// <summary>
/// 商品档案应用服务实现
/// 商品多态模型：主表 Product 区分类型（Type=1实物/2服务/3耗材/4样品/5赠品）
/// 仅服务项目（Type=2）有 ServiceProduct 子表存储特有字段
/// </summary>
public class ProductAppService : IProductAppService
{
    private readonly StoreDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<ProductCreateDto> _createValidator;
    private readonly IValidator<ProductUpdateDto> _updateValidator;

    public ProductAppService(
        StoreDbContext dbContext,
        ICurrentUser currentUser,
        IValidator<ProductCreateDto> createValidator,
        IValidator<ProductUpdateDto> updateValidator)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>
    /// 获取商品分页列表（含子表字段）
    /// </summary>
    public async Task<ApiResponseDto<PagedResponseDto<ProductDto>>> GetPagedListAsync(ProductQueryDto query)
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return ApiResponseDto<PagedResponseDto<ProductDto>>.Fail("无法确定当前租户", 401);
        }

        var tenantId = _currentUser.TenantId.Value;
        var queryable = _dbContext.Products
            .Include(p => p.Category)
            .Where(p => !p.IsDeleted && p.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(query.Name))
        {
            queryable = queryable.Where(p => p.Name.Contains(query.Name));
        }
        if (!string.IsNullOrWhiteSpace(query.Code))
        {
            queryable = queryable.Where(p => p.Code.Contains(query.Code));
        }
        if (query.CategoryId.HasValue)
        {
            queryable = queryable.Where(p => p.CategoryId == query.CategoryId.Value);
        }
        if (query.SupplierId.HasValue)
        {
            queryable = queryable.Where(p => p.SupplierId == query.SupplierId.Value);
        }
        if (query.Status.HasValue)
        {
            queryable = queryable.Where(p => p.Status == query.Status.Value);
        }
        if (query.Type.HasValue)
        {
            queryable = queryable.Where(p => p.Type == query.Type.Value);
        }

        var total = await queryable.CountAsync();
        var items = await queryable
            .OrderByDescending(p => p.CreatedTime)
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        // 先 Adapt 为 DTO 列表，再批量填充子表字段，避免子表字段丢失
        var dtoList = items.Adapt<List<ProductDto>>();
        await FillSubTableFieldsAsync(dtoList, tenantId);

        var result = new PagedResponseDto<ProductDto>
        {
            List = dtoList,
            Total = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
        return ApiResponseDto<PagedResponseDto<ProductDto>>.Ok(result);
    }

    /// <summary>
    /// 根据ID获取商品详情（含子表字段）
    /// </summary>
    public async Task<ApiResponseDto<ProductDto?>> GetByIdAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return ApiResponseDto<ProductDto?>.Fail("无法确定当前租户", 401);
        }

        var tenantId = _currentUser.TenantId.Value;
        var product = await _dbContext.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted && p.TenantId == tenantId);
        if (product == null)
        {
            return ApiResponseDto<ProductDto?>.Fail("商品不存在", 404);
        }

        var dto = product.Adapt<ProductDto>();
        await FillSubTableFieldsForProductAsync(dto, product.Id, tenantId);
        return ApiResponseDto<ProductDto?>.Ok(dto);
    }

    /// <summary>
    /// 创建商品（同时创建对应子表记录）
    /// </summary>
    public async Task<ApiResponseDto<ProductDto>> CreateAsync(ProductCreateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return ApiResponseDto<ProductDto>.Fail("无法确定当前租户", 401);
        }

        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
        {
            return ApiResponseDto<ProductDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);
        }

        var tenantId = _currentUser.TenantId.Value;
        var codeExists = await _dbContext.Products
            .AnyAsync(p => p.Code == dto.Code && p.TenantId == tenantId && !p.IsDeleted);
        if (codeExists)
        {
            return ApiResponseDto<ProductDto>.Fail($"商品编码 {dto.Code} 已存在", 400);
        }

        var product = dto.Adapt<ProductEntity>();
        product.TenantId = tenantId;
        product.TenantCode = _currentUser.TenantCode ?? string.Empty;
        product.CreatedTime = DateTime.Now;
        // 样品/赠品（Type=4/5）强制不可销售，其他类型默认可销售（B6.1 "不可销售"标识）
        product.IsSalable = dto.Type != 4 && dto.Type != 5;

        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        // 重新加载导航属性以获取 CategoryName
        await _dbContext.Entry(product).Reference(p => p.Category).LoadAsync();

        // 根据类型创建子表记录
        await CreateSubTableAsync(dto, product.Id, tenantId);

        var dtoResult = product.Adapt<ProductDto>();
        await FillSubTableFieldsForProductAsync(dtoResult, product.Id, tenantId);
        return ApiResponseDto<ProductDto>.Ok(dtoResult, "创建成功");
    }

    /// <summary>
    /// 更新商品（同时更新对应子表记录，处理类型变更）
    /// </summary>
    public async Task<ApiResponseDto<ProductDto>> UpdateAsync(ProductUpdateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return ApiResponseDto<ProductDto>.Fail("无法确定当前租户", 401);
        }

        var validation = await _updateValidator.ValidateAsync(dto);
        if (!validation.IsValid)
        {
            return ApiResponseDto<ProductDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);
        }

        var tenantId = _currentUser.TenantId.Value;
        var product = await _dbContext.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == dto.Id && !p.IsDeleted && p.TenantId == tenantId);
        if (product == null)
        {
            return ApiResponseDto<ProductDto>.Fail("商品不存在", 404);
        }

        // 编码变更时检查唯一性
        if (product.Code != dto.Code)
        {
            var codeExists = await _dbContext.Products
                .AnyAsync(p => p.Code == dto.Code && p.TenantId == tenantId && !p.IsDeleted && p.Id != dto.Id);
            if (codeExists)
            {
                return ApiResponseDto<ProductDto>.Fail($"商品编码 {dto.Code} 已存在", 400);
            }
        }

        // 记录原始类型，用于判断是否需要清理旧子表
        var oldType = product.Type;
        // 记录原始价格，用于价格变更日志
        var oldPrice = product.Price;

        // 手动更新主表字段（避免覆盖审计字段）
        product.Name = dto.Name;
        product.Code = dto.Code;
        product.CategoryId = dto.CategoryId;
        product.Type = dto.Type;
        product.Specification = dto.Spec;
        product.Unit = dto.Unit;
        product.Brand = dto.Brand;
        product.SupplierId = dto.SupplierId;
        product.Price = dto.Price;
        product.CostPrice = dto.CostPrice;
        product.LowStockThreshold = dto.LowStockThreshold;
        product.ExpiryAlertDays = dto.ExpiryAlertDays;
        product.OverstockThreshold = dto.OverstockThreshold;
        product.ImageUrl = dto.ImageUrl;
        product.Status = dto.Status;
        product.Remark = dto.Description;
        // 样品/赠品（Type=4/5）强制不可销售，与类型保持一致（B6.1 "不可销售"标识）
        product.IsSalable = dto.Type != 4 && dto.Type != 5;
        product.UpdatedTime = DateTime.Now;

        // 价格变更时自动记录 PriceChangeLog（G2.5）
        if (oldPrice != dto.Price)
        {
            _dbContext.PriceChangeLogs.Add(new PriceChangeLog
            {
                ProductId = product.Id,
                OldPrice = oldPrice,
                NewPrice = dto.Price,
                ChangeTime = DateTime.Now,
                OperatorId = _currentUser.UserId,
                Remark = "商品编辑自动记录",
                TenantId = tenantId,
                CreatedTime = DateTime.Now
            });
        }

        await _dbContext.SaveChangesAsync();

        // 类型变更时清理旧子表，再创建新子表；类型不变时更新现有子表
        if (oldType != dto.Type)
        {
            await DeleteSubTableAsync(oldType, dto.Id, tenantId);
            await CreateSubTableAsync(dto, dto.Id, tenantId);
        }
        else
        {
            await UpdateSubTableAsync(dto, dto.Id, tenantId);
        }

        var dtoResult = product.Adapt<ProductDto>();
        await FillSubTableFieldsForProductAsync(dtoResult, dto.Id, tenantId);
        return ApiResponseDto<ProductDto>.Ok(dtoResult, "更新成功");
    }

    /// <summary>
    /// 删除商品（软删除，同时软删除子表）
    /// </summary>
    public async Task<ApiResponseDto> DeleteAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return ApiResponseDto.Fail("无法确定当前租户", 401);
        }

        var tenantId = _currentUser.TenantId.Value;
        var product = await _dbContext.Products
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted && p.TenantId == tenantId);
        if (product == null)
        {
            return ApiResponseDto.Fail("商品不存在", 404);
        }

        product.IsDeleted = true;
        product.UpdatedTime = DateTime.Now;

        // 软删除子表
        await SoftDeleteSubTableAsync(product.Type, id, tenantId);

        await _dbContext.SaveChangesAsync();

        return ApiResponseDto.Success(null, "删除成功");
    }

    /// <summary>
    /// 批量删除商品（软删除，同时软删除子表）
    /// </summary>
    public async Task<ApiResponseDto> BatchDeleteAsync(List<long> ids)
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return ApiResponseDto.Fail("无法确定当前租户", 401);
        }

        if (ids == null || !ids.Any())
        {
            return ApiResponseDto.Fail("请选择要删除的商品", 400);
        }

        var tenantId = _currentUser.TenantId.Value;
        var products = await _dbContext.Products
            .Where(p => ids.Contains(p.Id) && !p.IsDeleted && p.TenantId == tenantId)
            .ToListAsync();

        foreach (var product in products)
        {
            product.IsDeleted = true;
            product.UpdatedTime = DateTime.Now;
            await SoftDeleteSubTableAsync(product.Type, product.Id, tenantId);
        }

        await _dbContext.SaveChangesAsync();

        return ApiResponseDto.Success(null, $"成功删除 {products.Count} 个商品");
    }

    // ========== 品项-供应商关联（G2.6.2）==========

    /// <summary>
    /// 查询品项关联的供应商列表（按 IsDefault 倒序，默认供应商排在首位）
    /// </summary>
    public async Task<ApiResponseDto<List<ProductSupplierDto>>> GetSuppliersByProductAsync(long productId)
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return ApiResponseDto<List<ProductSupplierDto>>.Fail("无法确定当前租户", 401);
        }

        var tenantId = _currentUser.TenantId.Value;
        var productExists = await _dbContext.Products
            .AnyAsync(p => p.Id == productId && !p.IsDeleted && p.TenantId == tenantId);
        if (!productExists)
        {
            return ApiResponseDto<List<ProductSupplierDto>>.Fail("商品不存在", 404);
        }

        var relations = await _dbContext.ProductSuppliers
            .Where(ps => ps.ProductId == productId && ps.TenantId == tenantId)
            .OrderByDescending(ps => ps.IsDefault)
            .ThenByDescending(ps => ps.CreatedTime)
            .ToListAsync();
        if (!relations.Any())
        {
            return ApiResponseDto<List<ProductSupplierDto>>.Ok(new List<ProductSupplierDto>());
        }

        var supplierIds = relations.Select(ps => ps.SupplierId).ToList();
        // 过滤已软删除供应商，避免展示无效关联；按租户隔离查询
        var suppliersInfo = await _dbContext.Suppliers
            .Where(s => supplierIds.Contains(s.Id) && s.TenantId == tenantId && !s.IsDeleted)
            .Select(s => new { s.Id, s.Code, s.Name })
            .ToListAsync();

        var result = relations.Select(ps =>
        {
            var s = suppliersInfo.FirstOrDefault(x => x.Id == ps.SupplierId);
            return new ProductSupplierDto
            {
                Id = ps.Id,
                ProductId = ps.ProductId,
                SupplierId = ps.SupplierId,
                SupplierCode = s?.Code,
                SupplierName = s?.Name,
                IsDefault = ps.IsDefault,
                ReferencePrice = ps.ReferencePrice,
                LeadTimeDays = ps.LeadTimeDays,
                CreatedAt = ps.CreatedTime,
                UpdatedAt = ps.UpdatedTime
            };
        }).ToList();
        return ApiResponseDto<List<ProductSupplierDto>>.Ok(result);
    }

    // ========== 子表辅助方法 ==========

    /// <summary>
    /// 批量填充子表字段到商品 DTO 列表
    /// </summary>
    private async Task FillSubTableFieldsAsync(List<ProductDto> dtos, long tenantId)
    {
        if (!dtos.Any()) return;

        // 按类型分组，批量查询子表数据，避免 N+1 查询
        var type2Ids = dtos.Where(d => d.Type == 2).Select(d => d.Id).ToList();

        if (type2Ids.Any())
        {
            var services = await _dbContext.ServiceProducts
                .Where(p => type2Ids.Contains(p.ProductId) && !p.IsDeleted && p.TenantId == tenantId)
                .ToListAsync();
            var serviceDict = services.ToDictionary(p => p.ProductId);
            var serviceIds = services.Select(s => s.Id).ToList();

            // 批量查询所需设备类型关联 + 设备类型名称
            var equipmentTypes = await _dbContext.ServiceProductEquipments
                .Where(r => serviceIds.Contains(r.ServiceProductId) && !r.IsDeleted)
                .Join(_dbContext.EquipmentTypes,
                    r => r.EquipmentTypeId,
                    e => e.Id,
                    (r, e) => new { r.ServiceProductId, EquipmentTypeId = r.EquipmentTypeId, EquipmentTypeName = e.Name })
                .ToListAsync();

            var equipmentTypesByService = equipmentTypes.GroupBy(x => x.ServiceProductId)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var dto in dtos.Where(d => d.Type == 2))
            {
                if (serviceDict.TryGetValue(dto.Id, out var s))
                {
                    dto.Duration = s.Duration;
                    dto.RequiredRoomType = s.RequiredRoomType;
                    dto.ApplicableSkills = s.ApplicableSkills;

                    if (equipmentTypesByService.TryGetValue(s.Id, out var list))
                    {
                        dto.EquipmentTypeIds = list.Select(x => x.EquipmentTypeId).ToList();
                        dto.EquipmentTypeNames = list.Select(x => x.EquipmentTypeName).ToList();
                    }
                }
            }
        }
    }

    /// <summary>
    /// 填充单个商品的子表字段
    /// </summary>
    private async Task FillSubTableFieldsForProductAsync(ProductDto dto, long productId, long tenantId)
    {
        switch (dto.Type)
        {
            case 2: // 服务项目
                var service = await _dbContext.ServiceProducts
                    .FirstOrDefaultAsync(p => p.ProductId == productId && !p.IsDeleted && p.TenantId == tenantId);
                if (service != null)
                {
                    dto.Duration = service.Duration;
                    dto.RequiredRoomType = service.RequiredRoomType;
                    dto.ApplicableSkills = service.ApplicableSkills;

                    // 查询所需设备类型关联 + 设备类型名称
                    var equipmentTypes = await _dbContext.ServiceProductEquipments
                        .Where(r => r.ServiceProductId == service.Id && !r.IsDeleted)
                        .Join(_dbContext.EquipmentTypes,
                            r => r.EquipmentTypeId,
                            e => e.Id,
                            (r, e) => new { EquipmentTypeId = r.EquipmentTypeId, EquipmentTypeName = e.Name })
                        .ToListAsync();
                    dto.EquipmentTypeIds = equipmentTypes.Select(x => x.EquipmentTypeId).ToList();
                    dto.EquipmentTypeNames = equipmentTypes.Select(x => x.EquipmentTypeName).ToList();
                }
                break;
        }
    }

    /// <summary>
    /// 根据商品类型创建子表记录
    /// </summary>
    private async Task CreateSubTableAsync(ProductCreateDto dto, long productId, long tenantId)
    {
        var tenantCode = _currentUser.TenantCode ?? string.Empty;
        var storeId = _currentUser.StoreId ?? 0;
        var storeCode = ""; // StoreCode 由前端 X-Store-Id 上下文确定，这里留空（用于审计）
        switch (dto.Type)
        {
            case 2: // 服务项目
                var service = new ServiceProductEntity
                {
                    ProductId = productId,
                    Duration = dto.Duration,
                    RequiredRoomType = dto.RequiredRoomType,
                    ApplicableSkills = dto.ApplicableSkills,
                    TenantId = tenantId,
                    TenantCode = tenantCode,
                    StoreId = storeId,
                    StoreCode = storeCode,
                    CreatedTime = DateTime.Now
                };
                _dbContext.ServiceProducts.Add(service);
                await _dbContext.SaveChangesAsync();

                // 级联创建所需设备类型关联
                if (dto.EquipmentTypeIds != null && dto.EquipmentTypeIds.Any())
                {
                    foreach (var equipmentTypeId in dto.EquipmentTypeIds.Distinct())
                    {
                        _dbContext.ServiceProductEquipments.Add(new ServiceProductEquipment
                        {
                            ServiceProductId = service.Id,
                            EquipmentTypeId = equipmentTypeId,
                            TenantId = tenantId,
                            TenantCode = tenantCode,
                            StoreId = storeId,
                            StoreCode = storeCode,
                            CreatedTime = DateTime.Now
                        });
                    }
                    await _dbContext.SaveChangesAsync();
                }
                break;
        }
    }

    /// <summary>
    /// 根据商品类型更新子表记录（类型不变场景）
    /// </summary>
    private async Task UpdateSubTableAsync(ProductCreateDto dto, long productId, long tenantId)
    {
        switch (dto.Type)
        {
            case 2: // 服务项目
                var service = await _dbContext.ServiceProducts
                    .FirstOrDefaultAsync(p => p.ProductId == productId && !p.IsDeleted && p.TenantId == tenantId);
                if (service != null)
                {
                    service.Duration = dto.Duration;
                    service.RequiredRoomType = dto.RequiredRoomType;
                    service.ApplicableSkills = dto.ApplicableSkills;
                    service.UpdatedTime = DateTime.Now;

                    // 替换所需设备类型关联：软删除旧记录 + 创建新记录
                    var oldRelations = await _dbContext.ServiceProductEquipments
                        .Where(r => r.ServiceProductId == service.Id && !r.IsDeleted)
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
                            _dbContext.ServiceProductEquipments.Add(new ServiceProductEquipment
                            {
                                ServiceProductId = service.Id,
                                EquipmentTypeId = equipmentTypeId,
                                TenantId = tenantId,
                                TenantCode = tenantCode,
                                StoreId = storeId,
                                StoreCode = "",
                                CreatedTime = DateTime.Now
                            });
                        }
                    }
                }
                else
                {
                    await CreateSubTableAsync(dto, productId, tenantId);
                }
                break;
        }
        await _dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// 根据旧的商品类型软删除子表记录（类型变更场景）
    /// </summary>
    private async Task DeleteSubTableAsync(int oldType, long productId, long tenantId)
    {
        switch (oldType)
        {
            case 2: // 服务项目
                var service = await _dbContext.ServiceProducts
                    .FirstOrDefaultAsync(p => p.ProductId == productId && !p.IsDeleted && p.TenantId == tenantId);
                if (service != null)
                {
                    service.IsDeleted = true;
                    service.UpdatedTime = DateTime.Now;

                    // 级联软删除所需设备类型关联
                    var relations = await _dbContext.ServiceProductEquipments
                        .Where(r => r.ServiceProductId == service.Id && !r.IsDeleted)
                        .ToListAsync();
                    foreach (var r in relations)
                    {
                        r.IsDeleted = true;
                        r.UpdatedTime = DateTime.Now;
                    }
                }
                break;
        }
        await _dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// 软删除子表记录（删除商品时调用）
    /// </summary>
    private async Task SoftDeleteSubTableAsync(int type, long productId, long tenantId)
    {
        switch (type)
        {
            case 2:
                var service = await _dbContext.ServiceProducts
                    .FirstOrDefaultAsync(p => p.ProductId == productId && !p.IsDeleted && p.TenantId == tenantId);
                if (service != null)
                {
                    service.IsDeleted = true;
                    service.UpdatedTime = DateTime.Now;

                    var relations = await _dbContext.ServiceProductEquipments
                        .Where(r => r.ServiceProductId == service.Id && !r.IsDeleted)
                        .ToListAsync();
                    foreach (var r in relations)
                    {
                        r.IsDeleted = true;
                        r.UpdatedTime = DateTime.Now;
                    }
                }
                break;
        }
    }
}
