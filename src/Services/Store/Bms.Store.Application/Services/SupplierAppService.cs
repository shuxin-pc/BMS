using Mapster;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Bms.BuildingBlocks.Abstractions.Security;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Suppliers;
using Bms.Store.Domain.Entities;
using SupplierEntity = Bms.Store.Domain.Entities.Supplier;
using Bms.Store.Infrastructure;

namespace Bms.Store.Application.Services;

/// <summary>
/// 供应商应用服务实现
/// </summary>
public class SupplierAppService : ISupplierAppService
{
    private readonly StoreDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<SupplierCreateDto> _createValidator;
    private readonly IValidator<SupplierUpdateDto> _updateValidator;
    private readonly IValidator<BindProductsDto> _bindProductsValidator;
    private readonly IValidator<SetDefaultSupplierDto> _setDefaultSupplierValidator;

    public SupplierAppService(
        StoreDbContext dbContext,
        ICurrentUser currentUser,
        IValidator<SupplierCreateDto> createValidator,
        IValidator<SupplierUpdateDto> updateValidator,
        IValidator<BindProductsDto> bindProductsValidator,
        IValidator<SetDefaultSupplierDto> setDefaultSupplierValidator)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _bindProductsValidator = bindProductsValidator;
        _setDefaultSupplierValidator = setDefaultSupplierValidator;
    }

    /// <summary>
    /// 获取供应商分页列表
    /// </summary>
    public async Task<ApiResponseDto<PagedResponseDto<SupplierDto>>> GetPagedListAsync(SupplierQueryDto query)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<PagedResponseDto<SupplierDto>>.Fail("无法确定当前租户", 401);

        var tenantId = _currentUser.TenantId.Value;
        var queryable = _dbContext.Suppliers
            .Where(s => !s.IsDeleted && s.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(query.Name))
            queryable = queryable.Where(s => s.Name.Contains(query.Name));
        if (!string.IsNullOrWhiteSpace(query.Code))
            queryable = queryable.Where(s => s.Code.Contains(query.Code));
        if (query.Status.HasValue)
            queryable = queryable.Where(s => s.Status == query.Status.Value);

        var total = await queryable.CountAsync();
        var items = await queryable
            .OrderByDescending(s => s.CreatedTime)
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        // 计算每个供应商的累计采购额
        var supplierIds = items.Select(s => s.Id).ToList();
        var purchaseAmounts = await _dbContext.PurchaseOrders
            .Where(p => supplierIds.Contains(p.SupplierId))
            .GroupBy(p => p.SupplierId)
            .Select(g => new { SupplierId = g.Key, TotalAmount = g.Sum(p => p.TotalAmount) })
            .ToListAsync();

        var dtos = items.Adapt<List<SupplierDto>>();
        foreach (var dto in dtos)
        {
            var amount = purchaseAmounts.FirstOrDefault(a => a.SupplierId == dto.Id);
            dto.TotalPurchaseAmount = amount?.TotalAmount ?? 0;
        }

        var result = new PagedResponseDto<SupplierDto>
        {
            List = dtos,
            Total = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
        return ApiResponseDto<PagedResponseDto<SupplierDto>>.Ok(result);
    }

    /// <summary>
    /// 根据ID获取供应商详情
    /// </summary>
    public async Task<ApiResponseDto<SupplierDto?>> GetByIdAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<SupplierDto?>.Fail("无法确定当前租户", 401);

        var entity = await _dbContext.Suppliers
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted && s.TenantId == _currentUser.TenantId.Value);
        if (entity == null)
            return ApiResponseDto<SupplierDto?>.Fail("供应商不存在", 404);

        var dto = entity.Adapt<SupplierDto>();
        dto.TotalPurchaseAmount = await _dbContext.PurchaseOrders
            .Where(p => p.SupplierId == id)
            .SumAsync(p => p.TotalAmount);

        return ApiResponseDto<SupplierDto?>.Ok(dto);
    }

    /// <summary>
    /// 创建供应商
    /// </summary>
    public async Task<ApiResponseDto<SupplierDto>> CreateAsync(SupplierCreateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<SupplierDto>.Fail("无法确定当前租户", 401);

        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<SupplierDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var codeExists = await _dbContext.Suppliers
            .AnyAsync(s => s.Code == dto.Code && s.TenantId == tenantId && !s.IsDeleted);
        if (codeExists)
            return ApiResponseDto<SupplierDto>.Fail($"供应商编码 {dto.Code} 已存在", 400);

        var entity = dto.Adapt<SupplierEntity>();
        entity.TenantId = tenantId;
        entity.TenantCode = _currentUser.TenantCode ?? string.Empty;
        entity.CreatedTime = DateTime.Now;

        _dbContext.Suppliers.Add(entity);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto<SupplierDto>.Ok(entity.Adapt<SupplierDto>(), "创建成功");
    }

    /// <summary>
    /// 更新供应商
    /// </summary>
    public async Task<ApiResponseDto<SupplierDto>> UpdateAsync(SupplierUpdateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<SupplierDto>.Fail("无法确定当前租户", 401);

        var validation = await _updateValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<SupplierDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var entity = await _dbContext.Suppliers
            .FirstOrDefaultAsync(s => s.Id == dto.Id && !s.IsDeleted && s.TenantId == tenantId);
        if (entity == null)
            return ApiResponseDto<SupplierDto>.Fail("供应商不存在", 404);

        // 编码变更时检查唯一性
        if (entity.Code != dto.Code)
        {
            var codeExists = await _dbContext.Suppliers
                .AnyAsync(s => s.Code == dto.Code && s.TenantId == tenantId && !s.IsDeleted && s.Id != dto.Id);
            if (codeExists)
                return ApiResponseDto<SupplierDto>.Fail($"供应商编码 {dto.Code} 已存在", 400);
        }

        entity.Name = dto.Name;
        entity.Code = dto.Code;
        entity.Contact = dto.Contact;
        entity.Phone = dto.Phone;
        entity.Address = dto.Address;
        entity.BankAccount = dto.BankAccount;
        entity.Status = dto.Status;
        entity.Remark = dto.Remark;
        entity.UpdatedTime = DateTime.Now;

        await _dbContext.SaveChangesAsync();
        return ApiResponseDto<SupplierDto>.Ok(entity.Adapt<SupplierDto>(), "更新成功");
    }

    /// <summary>
    /// 删除供应商（软删除）
    /// </summary>
    public async Task<ApiResponseDto> DeleteAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("无法确定当前租户", 401);

        var entity = await _dbContext.Suppliers
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted && s.TenantId == _currentUser.TenantId.Value);
        if (entity == null)
            return ApiResponseDto.Fail("供应商不存在", 404);

        entity.IsDeleted = true;
        entity.UpdatedTime = DateTime.Now;
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, "删除成功");
    }

    /// <summary>
    /// 批量删除供应商（软删除）
    /// </summary>
    public async Task<ApiResponseDto> BatchDeleteAsync(List<long> ids)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("无法确定当前租户", 401);
        if (ids == null || !ids.Any())
            return ApiResponseDto.Fail("请选择要删除的数据", 400);

        var entities = await _dbContext.Suppliers
            .Where(s => ids.Contains(s.Id) && !s.IsDeleted && s.TenantId == _currentUser.TenantId.Value)
            .ToListAsync();

        foreach (var entity in entities)
        {
            entity.IsDeleted = true;
            entity.UpdatedTime = DateTime.Now;
        }
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, $"成功删除 {entities.Count} 条数据");
    }

    // ========== 供应商-品项关联（G2.6.2）==========

    /// <summary>
    /// 批量绑定品项到供应商
    /// 业务规则：已存在的关联跳过；品项无默认供应商时将首个绑定设为默认，同步写入 Product.SupplierId 冗余字段
    /// </summary>
    public async Task<ApiResponseDto<List<ProductSupplierDto>>> BindProductsAsync(BindProductsDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<List<ProductSupplierDto>>.Fail("无法确定当前租户", 401);

        var validation = await _bindProductsValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<List<ProductSupplierDto>>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        if (dto.ProductIds == null || !dto.ProductIds.Any())
            return ApiResponseDto<List<ProductSupplierDto>>.Fail("请选择要绑定的品项", 400);

        var tenantId = _currentUser.TenantId.Value;
        var tenantCode = _currentUser.TenantCode ?? string.Empty;
        var now = DateTime.Now;

        // 校验供应商存在
        var supplierExists = await _dbContext.Suppliers
            .AnyAsync(s => s.Id == dto.SupplierId && !s.IsDeleted && s.TenantId == tenantId);
        if (!supplierExists)
            return ApiResponseDto<List<ProductSupplierDto>>.Fail("供应商不存在", 404);

        // 校验所有品项存在（去重后查询）
        var productIds = dto.ProductIds.Distinct().ToList();
        var existingProductIds = await _dbContext.Products
            .Where(p => productIds.Contains(p.Id) && !p.IsDeleted && p.TenantId == tenantId)
            .Select(p => p.Id)
            .ToListAsync();
        var invalidProductIds = productIds.Except(existingProductIds).ToList();
        if (invalidProductIds.Any())
            return ApiResponseDto<List<ProductSupplierDto>>.Fail($"以下品项不存在：{string.Join(",", invalidProductIds)}", 404);

        // 查询已存在的关联（避免重复绑定）
        var existingRelations = await _dbContext.ProductSuppliers
            .Where(ps => ps.SupplierId == dto.SupplierId
                && productIds.Contains(ps.ProductId)
                && ps.TenantId == tenantId)
            .ToListAsync();
        var existingProductIdsBound = existingRelations.Select(ps => ps.ProductId).ToHashSet();

        var toCreate = productIds.Except(existingProductIdsBound).ToList();
        var createdEntities = new List<ProductSupplier>();
        foreach (var productId in toCreate)
        {
            // 品项当前是否已有默认供应商（决定本次绑定是否设为默认）
            var hasDefault = await _dbContext.ProductSuppliers
                .AnyAsync(ps => ps.ProductId == productId && ps.IsDefault && ps.TenantId == tenantId);
            var isDefault = !hasDefault;

            var relation = new ProductSupplier
            {
                ProductId = productId,
                SupplierId = dto.SupplierId,
                IsDefault = isDefault,
                ReferencePrice = null,
                LeadTimeDays = 0,
                TenantId = tenantId,
                TenantCode = tenantCode,
                CreatedTime = now
            };
            _dbContext.ProductSuppliers.Add(relation);
            createdEntities.Add(relation);

            // 设为默认时同步写入 Product.SupplierId 冗余字段
            if (isDefault)
            {
                var product = await _dbContext.Products
                    .FirstOrDefaultAsync(p => p.Id == productId && p.TenantId == tenantId);
                if (product != null)
                {
                    product.SupplierId = dto.SupplierId;
                    product.UpdatedTime = now;
                }
            }
        }

        if (createdEntities.Any())
            await _dbContext.SaveChangesAsync();

        // 返回本次绑定的关联记录（含品项编码/名称），按租户隔离查询
        var boundProductIds = createdEntities.Select(ps => ps.ProductId).ToList();
        var productsInfo = await _dbContext.Products
            .Where(p => boundProductIds.Contains(p.Id) && p.TenantId == tenantId)
            .Select(p => new { p.Id, p.Code, p.Name })
            .ToListAsync();
        var supplierInfo = await _dbContext.Suppliers
            .Where(s => s.Id == dto.SupplierId && s.TenantId == tenantId)
            .Select(s => new { s.Code, s.Name })
            .FirstOrDefaultAsync();
        if (supplierInfo == null)
            return ApiResponseDto<List<ProductSupplierDto>>.Fail("供应商不存在", 404);

        var result = createdEntities.Select(ps =>
        {
            var p = productsInfo.FirstOrDefault(x => x.Id == ps.ProductId);
            return new ProductSupplierDto
            {
                Id = ps.Id,
                ProductId = ps.ProductId,
                ProductCode = p?.Code,
                ProductName = p?.Name,
                SupplierId = ps.SupplierId,
                SupplierCode = supplierInfo.Code,
                SupplierName = supplierInfo.Name,
                IsDefault = ps.IsDefault,
                ReferencePrice = ps.ReferencePrice,
                LeadTimeDays = ps.LeadTimeDays,
                CreatedAt = ps.CreatedTime,
                UpdatedAt = ps.UpdatedTime
            };
        }).ToList();

        var skippedCount = existingProductIdsBound.Count;
        var message = skippedCount > 0
            ? $"成功绑定 {result.Count} 个品项，跳过 {skippedCount} 个已绑定品项"
            : $"成功绑定 {result.Count} 个品项";
        return ApiResponseDto<List<ProductSupplierDto>>.Ok(result, message);
    }

    /// <summary>
    /// 解除品项与供应商的关联
    /// 业务规则：物理删除关联记录；若解除的是默认供应商，自动将下一个关联设为新默认并同步 Product.SupplierId
    /// </summary>
    public async Task<ApiResponseDto> UnbindProductAsync(long productId, long supplierId)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("无法确定当前租户", 401);

        var tenantId = _currentUser.TenantId.Value;
        var relation = await _dbContext.ProductSuppliers
            .FirstOrDefaultAsync(ps => ps.ProductId == productId
                && ps.SupplierId == supplierId
                && ps.TenantId == tenantId);
        if (relation == null)
            return ApiResponseDto.Fail("品项与供应商未建立关联", 404);

        var wasDefault = relation.IsDefault;
        _dbContext.ProductSuppliers.Remove(relation);

        // 解除默认供应商时，自动选择下一个关联作为新默认
        if (wasDefault)
        {
            var nextRelation = await _dbContext.ProductSuppliers
                .Where(ps => ps.ProductId == productId && ps.TenantId == tenantId && ps.SupplierId != supplierId)
                .OrderBy(ps => ps.CreatedTime)
                .FirstOrDefaultAsync();
            var product = await _dbContext.Products
                .FirstOrDefaultAsync(p => p.Id == productId && p.TenantId == tenantId);
            if (product != null)
            {
                product.SupplierId = nextRelation?.SupplierId;
                product.UpdatedTime = DateTime.Now;
            }
            if (nextRelation != null)
            {
                nextRelation.IsDefault = true;
                nextRelation.UpdatedTime = DateTime.Now;
            }
        }

        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, "解除关联成功");
    }

    /// <summary>
    /// 设置品项的默认供应商
    /// 业务规则：自动取消旧默认，同步写入 Product.SupplierId 冗余字段；可选更新参考价与供货周期
    /// </summary>
    public async Task<ApiResponseDto<ProductSupplierDto>> SetDefaultSupplierAsync(SetDefaultSupplierDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<ProductSupplierDto>.Fail("无法确定当前租户", 401);

        var validation = await _setDefaultSupplierValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<ProductSupplierDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var relation = await _dbContext.ProductSuppliers
            .FirstOrDefaultAsync(ps => ps.ProductId == dto.ProductId
                && ps.SupplierId == dto.SupplierId
                && ps.TenantId == tenantId);
        if (relation == null)
            return ApiResponseDto<ProductSupplierDto>.Fail("品项与供应商未建立关联，请先绑定", 404);

        var now = DateTime.Now;

        // 取消旧默认（一个品项仅一个默认供应商）
        var oldDefaults = await _dbContext.ProductSuppliers
            .Where(ps => ps.ProductId == dto.ProductId && ps.IsDefault && ps.SupplierId != dto.SupplierId && ps.TenantId == tenantId)
            .ToListAsync();
        foreach (var old in oldDefaults)
        {
            old.IsDefault = false;
            old.UpdatedTime = now;
        }

        relation.IsDefault = true;
        if (dto.ReferencePrice.HasValue)
            relation.ReferencePrice = dto.ReferencePrice;
        if (dto.LeadTimeDays.HasValue)
            relation.LeadTimeDays = dto.LeadTimeDays.Value;
        relation.UpdatedTime = now;

        // 同步 Product.SupplierId 冗余字段，保证采购下单时按 Product.SupplierId 过滤与默认供应商一致
        var product = await _dbContext.Products
            .FirstOrDefaultAsync(p => p.Id == dto.ProductId && p.TenantId == tenantId);
        if (product != null)
        {
            product.SupplierId = dto.SupplierId;
            product.UpdatedTime = now;
        }

        await _dbContext.SaveChangesAsync();

        // 组装返回 DTO（含品项与供应商编码/名称），按租户隔离查询
        var productInfo = await _dbContext.Products
            .Where(p => p.Id == dto.ProductId && p.TenantId == tenantId)
            .Select(p => new { p.Code, p.Name })
            .FirstOrDefaultAsync();
        var supplierInfo = await _dbContext.Suppliers
            .Where(s => s.Id == dto.SupplierId && s.TenantId == tenantId)
            .Select(s => new { s.Code, s.Name })
            .FirstOrDefaultAsync();

        var result = new ProductSupplierDto
        {
            Id = relation.Id,
            ProductId = relation.ProductId,
            ProductCode = productInfo?.Code,
            ProductName = productInfo?.Name,
            SupplierId = relation.SupplierId,
            SupplierCode = supplierInfo?.Code,
            SupplierName = supplierInfo?.Name,
            IsDefault = relation.IsDefault,
            ReferencePrice = relation.ReferencePrice,
            LeadTimeDays = relation.LeadTimeDays,
            CreatedAt = relation.CreatedTime,
            UpdatedAt = relation.UpdatedTime
        };
        return ApiResponseDto<ProductSupplierDto>.Ok(result, "设置默认供应商成功");
    }

    /// <summary>
    /// 查询供应商关联的品项列表（按 IsDefault 倒序，默认供应商排在首位）
    /// </summary>
    public async Task<ApiResponseDto<List<ProductSupplierDto>>> GetProductsBySupplierAsync(long supplierId)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<List<ProductSupplierDto>>.Fail("无法确定当前租户", 401);

        var tenantId = _currentUser.TenantId.Value;
        var supplierExists = await _dbContext.Suppliers
            .AnyAsync(s => s.Id == supplierId && !s.IsDeleted && s.TenantId == tenantId);
        if (!supplierExists)
            return ApiResponseDto<List<ProductSupplierDto>>.Fail("供应商不存在", 404);

        var relations = await _dbContext.ProductSuppliers
            .Where(ps => ps.SupplierId == supplierId && ps.TenantId == tenantId)
            .OrderByDescending(ps => ps.IsDefault)
            .ThenByDescending(ps => ps.CreatedTime)
            .ToListAsync();
        if (!relations.Any())
            return ApiResponseDto<List<ProductSupplierDto>>.Ok(new List<ProductSupplierDto>());

        var productIds = relations.Select(ps => ps.ProductId).ToList();
        // 过滤已软删除品项，避免展示无效关联；按租户隔离查询
        var productsInfo = await _dbContext.Products
            .Where(p => productIds.Contains(p.Id) && p.TenantId == tenantId && !p.IsDeleted)
            .Select(p => new { p.Id, p.Code, p.Name })
            .ToListAsync();

        var result = relations.Select(ps =>
        {
            var p = productsInfo.FirstOrDefault(x => x.Id == ps.ProductId);
            return new ProductSupplierDto
            {
                Id = ps.Id,
                ProductId = ps.ProductId,
                ProductCode = p?.Code,
                ProductName = p?.Name,
                SupplierId = ps.SupplierId,
                IsDefault = ps.IsDefault,
                ReferencePrice = ps.ReferencePrice,
                LeadTimeDays = ps.LeadTimeDays,
                CreatedAt = ps.CreatedTime,
                UpdatedAt = ps.UpdatedTime
            };
        }).ToList();
        return ApiResponseDto<List<ProductSupplierDto>>.Ok(result);
    }
}
