using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Bms.BuildingBlocks.Abstractions.Security;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Products;
using Bms.Store.Application.Dtos.Suppliers;
using Bms.Store.Domain.Entities;
using Bms.Store.Infrastructure;

namespace Bms.Store.Application.Services;

/// <summary>
/// 门店商品档案应用服务（门店隔离，承载分店差异化属性）
/// Master 字段通过 MasterId 关联 ProductMaster 获取，本服务仅管理 Store 字段
/// 服务商品子表（ServiceProduct）由 ProductMasterAppService 管理
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
    /// 获取门店商品档案分页列表（Join Master 获取 Master 字段）
    /// </summary>
    public async Task<ApiResponseDto<PagedResponseDto<ProductDto>>> GetPagedListAsync(ProductQueryDto query)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<PagedResponseDto<ProductDto>>.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId ?? 0;
        var queryable = _dbContext.Products
            .Include(p => p.Master).ThenInclude(m => m.Category)
            .Where(p => !p.IsDeleted && p.TenantId == tenantId && p.StoreId == storeId);

        // Name/Code/Type/CategoryId 改为查 Master
        if (!string.IsNullOrWhiteSpace(query.Name))
            queryable = queryable.Where(p => p.Master.Name.Contains(query.Name));
        if (!string.IsNullOrWhiteSpace(query.Code))
            queryable = queryable.Where(p => p.Master.Code.Contains(query.Code));
        if (query.Type.HasValue)
            queryable = queryable.Where(p => p.Master.Type == query.Type.Value);
        if (query.CategoryId.HasValue)
        {
            // 选中父级分类时包含其所有子孙分类（分类改租户级，不再按 StoreId 过滤）
            var targetId = query.CategoryId.Value;
            var allCategories = await _dbContext.ProductCategories
                .Where(c => c.TenantId == tenantId && !c.IsDeleted)
                .Select(c => new { c.Id, c.ParentId })
                .ToListAsync();
            var categoryIds = new HashSet<long> { targetId };
            var queue = new Queue<long>();
            queue.Enqueue(targetId);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var child in allCategories.Where(c => c.ParentId == current))
                {
                    if (categoryIds.Add(child.Id))
                        queue.Enqueue(child.Id);
                }
            }
            queryable = queryable.Where(p => categoryIds.Contains(p.Master.CategoryId));
        }
        if (query.Status.HasValue)
            queryable = queryable.Where(p => p.Status == query.Status.Value);

        var total = await queryable.CountAsync();
        var items = await queryable
            .OrderByDescending(p => p.CreatedTime)
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var dtoList = items.Select(MapToDto).ToList();
        await FillSubTableFieldsAsync(dtoList, tenantId);

        return ApiResponseDto<PagedResponseDto<ProductDto>>.Ok(new PagedResponseDto<ProductDto>
        {
            List = dtoList,
            Total = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        });
    }

    /// <summary>
    /// 根据ID获取门店商品档案详情（含 Master 字段 + 子表字段）
    /// </summary>
    public async Task<ApiResponseDto<ProductDto?>> GetByIdAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<ProductDto?>.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId ?? 0;
        var product = await _dbContext.Products
            .Include(p => p.Master).ThenInclude(m => m.Category)
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted && p.TenantId == tenantId && p.StoreId == storeId);
        if (product == null)
            return ApiResponseDto<ProductDto?>.Fail("商品不存在", 404);

        var dto = MapToDto(product);
        await FillSubTableFieldsForProductAsync(dto, product.MasterId, tenantId);
        return ApiResponseDto<ProductDto?>.Ok(dto);
    }

    /// <summary>
    /// 创建门店商品档案（基于已有 Master，仅 Store 字段）
    /// 业务约定：先建 Master，再建门店档案（设计文档 6.1 节）
    /// </summary>
    public async Task<ApiResponseDto<ProductDto>> CreateAsync(ProductCreateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<ProductDto>.Fail("登录状态异常，请重新登录", 401);
        if (!_currentUser.StoreId.HasValue)
            return ApiResponseDto<ProductDto>.Fail("无法确定当前门店", 401);

        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<ProductDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId.Value;

        // 校验 Master 存在
        var master = await _dbContext.ProductMasters
            .FirstOrDefaultAsync(m => m.Id == dto.MasterId && !m.IsDeleted && m.TenantId == tenantId);
        if (master == null)
            return ApiResponseDto<ProductDto>.Fail("商品主档不存在，请先创建主档", 404);

        // 校验同一门店同一 Master 不重复（唯一约束 TenantId+StoreId+MasterId）
        var exists = await _dbContext.Products
            .AnyAsync(p => p.MasterId == dto.MasterId && p.TenantId == tenantId
                && p.StoreId == storeId && !p.IsDeleted);
        if (exists)
            return ApiResponseDto<ProductDto>.Fail("该门店已存在此商品档案", 400);

        var product = new Product
        {
            MasterId = dto.MasterId,
            Price = dto.Price,
            CostPrice = dto.CostPrice,
            LowStockThreshold = dto.LowStockThreshold,
            ExpiryAlertDays = dto.ExpiryAlertDays,
            OverstockThreshold = dto.OverstockThreshold,
            Status = dto.Status,
            Remark = dto.Remark,
            TenantId = tenantId,
            TenantCode = _currentUser.TenantCode ?? string.Empty,
            StoreId = storeId,
            StoreCode = _currentUser.StoreCode ?? string.Empty,
            CreatedTime = DateTime.Now
        };
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        // 重新加载 Master + Category
        await _dbContext.Entry(product).Reference(p => p.Master).LoadAsync();
        await _dbContext.Entry(product.Master).Reference(m => m.Category).LoadAsync();

        var dtoResult = MapToDto(product);
        await FillSubTableFieldsForProductAsync(dtoResult, product.MasterId, tenantId);
        return ApiResponseDto<ProductDto>.Ok(dtoResult, "创建成功");
    }

    /// <summary>
    /// 更新门店商品档案（仅 Store 字段，Master 字段由 ProductMasterAppService 管理）
    /// </summary>
    public async Task<ApiResponseDto<ProductDto>> UpdateAsync(ProductUpdateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<ProductDto>.Fail("登录状态异常，请重新登录", 401);

        var validation = await _updateValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<ProductDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId ?? 0;
        var product = await _dbContext.Products
            .Include(p => p.Master).ThenInclude(m => m.Category)
            .FirstOrDefaultAsync(p => p.Id == dto.Id && !p.IsDeleted && p.TenantId == tenantId && p.StoreId == storeId);
        if (product == null)
            return ApiResponseDto<ProductDto>.Fail("商品不存在", 404);

        var oldPrice = product.Price;
        // 仅更新 Store 字段
        product.Price = dto.Price;
        product.CostPrice = dto.CostPrice;
        product.LowStockThreshold = dto.LowStockThreshold;
        product.ExpiryAlertDays = dto.ExpiryAlertDays;
        product.OverstockThreshold = dto.OverstockThreshold;
        product.Status = dto.Status;
        product.Remark = dto.Remark;
        product.UpdatedTime = DateTime.Now;

        // 价格变更记录（G2.5）
        if (oldPrice != dto.Price)
        {
            _dbContext.PriceChangeLogs.Add(new PriceChangeLog
            {
                ProductId = product.Id,
                OldPrice = oldPrice,
                NewPrice = dto.Price,
                ChangeTime = DateTime.Now,
                OperatorId = _currentUser.UserId,
                OperatorName = _currentUser.RealName ?? _currentUser.UserName,
                Remark = "商品编辑自动记录",
                TenantId = tenantId,
                CreatedTime = DateTime.Now
            });
        }

        await _dbContext.SaveChangesAsync();

        var dtoResult = MapToDto(product);
        await FillSubTableFieldsForProductAsync(dtoResult, product.MasterId, tenantId);
        return ApiResponseDto<ProductDto>.Ok(dtoResult, "更新成功");
    }

    /// <summary>
    /// 删除门店商品档案（软删除）
    /// 设计文档 8.3 节：Inventory.Quantity > 0 禁止删除，需先清空库存
    /// </summary>
    public async Task<ApiResponseDto> DeleteAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId ?? 0;
        var product = await _dbContext.Products
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted && p.TenantId == tenantId && p.StoreId == storeId);
        if (product == null)
            return ApiResponseDto.Fail("商品不存在", 404);

        // 库存检查：有库存禁止删除（设计文档 8.3 节）
        var hasStock = await _dbContext.Inventories
            .AnyAsync(inv => inv.ProductId == id && inv.TenantId == tenantId && inv.StoreId == storeId && inv.Quantity > 0);
        if (hasStock)
            return ApiResponseDto.Fail("该商品仍有库存，请先清空后再删除", 400);

        product.IsDeleted = true;
        product.UpdatedTime = DateTime.Now;
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, "删除成功");
    }

    /// <summary>
    /// 批量删除门店商品档案（软删除）
    /// 设计文档 8.3 节：有库存的商品跳过并提示
    /// </summary>
    public async Task<ApiResponseDto> BatchDeleteAsync(List<long> ids)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("登录状态异常，请重新登录", 401);

        if (ids == null || !ids.Any())
            return ApiResponseDto.Fail("请选择要删除的商品", 400);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId ?? 0;
        var products = await _dbContext.Products
            .Where(p => ids.Contains(p.Id) && !p.IsDeleted && p.TenantId == tenantId && p.StoreId == storeId)
            .ToListAsync();

        // 批量查询有库存的商品 ID（设计文档 8.3 节：有库存禁止删除）
        var productIds = products.Select(p => p.Id).ToList();
        var inStockIds = await _dbContext.Inventories
            .Where(inv => productIds.Contains(inv.ProductId) && inv.TenantId == tenantId && inv.StoreId == storeId && inv.Quantity > 0)
            .Select(inv => inv.ProductId)
            .ToListAsync();
        var inStockSet = inStockIds.ToHashSet();

        var deletable = products.Where(p => !inStockSet.Contains(p.Id)).ToList();
        var skipped = products.Count - deletable.Count;

        foreach (var product in deletable)
        {
            product.IsDeleted = true;
            product.UpdatedTime = DateTime.Now;
        }

        await _dbContext.SaveChangesAsync();

        var message = $"成功删除 {deletable.Count} 个商品";
        if (skipped > 0)
            message += $"，跳过 {skipped} 个仍有库存的商品（请先清空库存）";

        return ApiResponseDto.Success(null, message);
    }

    // ========== 品项-供应商关联（G2.6.2）==========

    /// <summary>
    /// 查询品项关联的供应商列表（按 IsDefault 倒序，默认供应商排在首位）
    /// </summary>
    public async Task<ApiResponseDto<List<ProductSupplierDto>>> GetSuppliersByProductAsync(long productId)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<List<ProductSupplierDto>>.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId ?? 0;
        var productExists = await _dbContext.Products
            .AnyAsync(p => p.Id == productId && !p.IsDeleted && p.TenantId == tenantId && p.StoreId == storeId);
        if (!productExists)
            return ApiResponseDto<List<ProductSupplierDto>>.Fail("商品不存在", 404);

        var relations = await _dbContext.ProductSuppliers
            .Where(ps => ps.ProductId == productId && ps.TenantId == tenantId && ps.StoreId == storeId)
            .OrderByDescending(ps => ps.IsDefault)
            .ThenByDescending(ps => ps.CreatedTime)
            .ToListAsync();
        if (!relations.Any())
            return ApiResponseDto<List<ProductSupplierDto>>.Ok(new List<ProductSupplierDto>());

        var supplierIds = relations.Select(ps => ps.SupplierId).ToList();
        // 过滤已软删除供应商，避免展示无效关联；含门店通用（Scope=1）和本门店私用（Scope=2）
        var suppliersInfo = await _dbContext.Suppliers
            .Where(s => supplierIds.Contains(s.Id) && s.TenantId == tenantId && !s.IsDeleted
                && (s.Scope == 1 || s.StoreId == storeId))
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

    /// <summary>
    /// 获取门店商品轻量选项列表（Join Master 获取 Name/Code/Unit）
    /// </summary>
    public async Task<ApiResponseDto<List<ProductOptionDto>>> GetOptionsAsync()
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<List<ProductOptionDto>>.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId ?? 0;

        var options = await _dbContext.Products
            .Where(p => !p.IsDeleted && p.TenantId == tenantId && p.StoreId == storeId)
            .Join(_dbContext.ProductMasters,
                p => p.MasterId,
                m => m.Id,
                (p, m) => new ProductOptionDto
                {
                    Id = p.Id,
                    Name = m.Name,
                    Code = m.Code,
                    Unit = m.Unit,
                    Type = m.Type
                })
            .OrderBy(x => x.Name)
            .ToListAsync();

        return ApiResponseDto<List<ProductOptionDto>>.Ok(options);
    }

    // ========== 辅助方法 ==========

    /// <summary>
    /// 将 Product 实体映射为 ProductDto（Master 字段从 Master 导航填充）
    /// </summary>
    private static ProductDto MapToDto(Product product)
    {
        return new ProductDto
        {
            Id = product.Id,
            MasterId = product.MasterId,
            // Master 字段
            Name = product.Master?.Name ?? string.Empty,
            Code = product.Master?.Code ?? string.Empty,
            Type = product.Master?.Type ?? 0,
            CategoryId = product.Master?.CategoryId ?? 0,
            CategoryName = product.Master?.Category?.Name,
            Unit = product.Master?.Unit,
            Spec = product.Master?.Specification,
            Brand = product.Master?.Brand,
            ImageUrl = product.Master?.ImageUrl,
            IsSalable = product.Master?.IsSalable ?? true,
            // Store 字段
            Price = product.Price,
            CostPrice = product.CostPrice,
            LastPurchasePrice = product.LastPurchasePrice,
            LowStockThreshold = product.LowStockThreshold,
            ExpiryAlertDays = product.ExpiryAlertDays,
            OverstockThreshold = product.OverstockThreshold,
            Status = product.Status,
            Remark = product.Remark,
            CreatedAt = product.CreatedTime,
            UpdatedAt = product.UpdatedTime
        };
    }

    /// <summary>
    /// 批量填充子表字段：默认供应商 + 服务项目子表（从 Master.ServiceProduct 获取）
    /// </summary>
    private async Task FillSubTableFieldsAsync(List<ProductDto> dtos, long tenantId)
    {
        if (!dtos.Any()) return;

        // 批量填充默认供应商（从 ProductSupplier.IsDefault=true 派生）
        var productIds = dtos.Select(d => d.Id).ToList();
        var defaultRelations = await _dbContext.ProductSuppliers
            .Where(ps => productIds.Contains(ps.ProductId) && ps.IsDefault && ps.TenantId == tenantId)
            .ToListAsync();
        if (defaultRelations.Any())
        {
            var defaultSupplierIds = defaultRelations.Select(ps => ps.SupplierId).Distinct().ToList();
            var suppliersInfo = await _dbContext.Suppliers
                .Where(s => defaultSupplierIds.Contains(s.Id) && !s.IsDeleted && s.TenantId == tenantId)
                .Select(s => new { s.Id, s.Name })
                .ToListAsync();
            var supplierDict = suppliersInfo.ToDictionary(s => s.Id);

            foreach (var dto in dtos)
            {
                var relation = defaultRelations.FirstOrDefault(ps => ps.ProductId == dto.Id);
                if (relation != null)
                {
                    dto.DefaultSupplierId = relation.SupplierId;
                    dto.DefaultSupplierName = supplierDict.TryGetValue(relation.SupplierId, out var s) ? s.Name : null;
                }
            }
        }

        // 批量填充服务子表字段（Type=2，从 Master.ServiceProduct 获取）
        var type2MasterIds = dtos.Where(d => d.Type == 2).Select(d => d.MasterId).Distinct().ToList();
        if (!type2MasterIds.Any()) return;

        var services = await _dbContext.ServiceProducts
            .Where(s => type2MasterIds.Contains(s.MasterId) && !s.IsDeleted && s.TenantId == tenantId)
            .ToListAsync();
        var serviceDict = services.ToDictionary(s => s.MasterId);
        var serviceIds = services.Select(s => s.Id).ToList();

        var equipmentTypes = await _dbContext.ServiceProductEquipments
            .Where(r => serviceIds.Contains(r.ServiceProductId))
            .Join(_dbContext.EquipmentTypes,
                r => r.EquipmentTypeId,
                e => e.Id,
                (r, e) => new { r.ServiceProductId, EquipmentTypeId = r.EquipmentTypeId, EquipmentTypeName = e.Name })
            .ToListAsync();
        var equipmentByService = equipmentTypes.GroupBy(x => x.ServiceProductId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // 适用技能关联（门店语境：仅当前门店的配置）
        var storeId = _currentUser.StoreId ?? 0;
        var skillRelations = await _dbContext.ServiceProductSkills
            .Where(r => serviceIds.Contains(r.ServiceProductId) && r.StoreId == storeId)
            .Join(_dbContext.SkillCategories,
                r => r.SkillCategoryId,
                c => c.Id,
                (r, c) => new { r.ServiceProductId, SkillCategoryId = r.SkillCategoryId, SkillCategoryName = c.Name })
            .ToListAsync();
        var skillByService = skillRelations.GroupBy(x => x.ServiceProductId)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var dto in dtos.Where(d => d.Type == 2))
        {
            if (serviceDict.TryGetValue(dto.MasterId, out var s))
            {
                dto.Duration = s.Duration;
                dto.RequiredRoomType = s.RequiredRoomType;
                if (equipmentByService.TryGetValue(s.Id, out var list))
                {
                    dto.EquipmentTypeIds = list.Select(x => x.EquipmentTypeId).ToList();
                    dto.EquipmentTypeNames = list.Select(x => x.EquipmentTypeName).ToList();
                }
                if (skillByService.TryGetValue(s.Id, out var skillList))
                {
                    dto.SkillCategoryIds = skillList.Select(x => x.SkillCategoryId).ToList();
                    dto.SkillCategoryNames = skillList.Select(x => x.SkillCategoryName).ToList();
                }
            }
        }
    }

    /// <summary>
    /// 填充单个商品的子表字段：默认供应商 + 服务项目子表
    /// </summary>
    private async Task FillSubTableFieldsForProductAsync(ProductDto dto, long masterId, long tenantId)
    {
        // 填充默认供应商
        var defaultRelation = await _dbContext.ProductSuppliers
            .Where(ps => ps.ProductId == dto.Id && ps.IsDefault && ps.TenantId == tenantId)
            .FirstOrDefaultAsync();
        if (defaultRelation != null)
        {
            dto.DefaultSupplierId = defaultRelation.SupplierId;
            var supplierInfo = await _dbContext.Suppliers
                .Where(s => s.Id == defaultRelation.SupplierId && !s.IsDeleted && s.TenantId == tenantId)
                .Select(s => new { s.Name })
                .FirstOrDefaultAsync();
            dto.DefaultSupplierName = supplierInfo?.Name;
        }

        // 服务子表字段（Type=2）
        if (dto.Type != 2) return;

        var service = await _dbContext.ServiceProducts
            .FirstOrDefaultAsync(s => s.MasterId == masterId && !s.IsDeleted && s.TenantId == tenantId);
        if (service == null) return;

        dto.Duration = service.Duration;
        dto.RequiredRoomType = service.RequiredRoomType;

        var equipmentTypes = await _dbContext.ServiceProductEquipments
            .Where(r => r.ServiceProductId == service.Id)
            .Join(_dbContext.EquipmentTypes,
                r => r.EquipmentTypeId,
                e => e.Id,
                (r, e) => new { EquipmentTypeId = r.EquipmentTypeId, EquipmentTypeName = e.Name })
            .ToListAsync();
        dto.EquipmentTypeIds = equipmentTypes.Select(x => x.EquipmentTypeId).ToList();
        dto.EquipmentTypeNames = equipmentTypes.Select(x => x.EquipmentTypeName).ToList();

        // 适用技能关联（门店语境：仅当前门店的配置）
        var storeId = _currentUser.StoreId ?? 0;
        var skillRelations = await _dbContext.ServiceProductSkills
            .Where(r => r.ServiceProductId == service.Id && r.StoreId == storeId)
            .Join(_dbContext.SkillCategories,
                r => r.SkillCategoryId,
                c => c.Id,
                (r, c) => new { SkillCategoryId = r.SkillCategoryId, SkillCategoryName = c.Name })
            .ToListAsync();
        dto.SkillCategoryIds = skillRelations.Select(x => x.SkillCategoryId).ToList();
        dto.SkillCategoryNames = skillRelations.Select(x => x.SkillCategoryName).ToList();
    }
}
