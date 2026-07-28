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
        if (!_currentUser.TenantId.HasValue || !_currentUser.StoreId.HasValue)
            return ApiResponseDto<PagedResponseDto<SupplierDto>>.Fail("无法确定当前门店", 401);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId.Value;
        var queryable = _dbContext.Suppliers
            .Where(s => !s.IsDeleted && s.TenantId == tenantId && s.StoreId == storeId);

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
        if (!_currentUser.TenantId.HasValue || !_currentUser.StoreId.HasValue)
            return ApiResponseDto<SupplierDto?>.Fail("无法确定当前门店", 401);

        var entity = await _dbContext.Suppliers
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted
                && s.TenantId == _currentUser.TenantId.Value
                && s.StoreId == _currentUser.StoreId.Value);
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
        if (!_currentUser.TenantId.HasValue || !_currentUser.StoreId.HasValue)
            return ApiResponseDto<SupplierDto>.Fail("无法确定当前门店", 401);

        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<SupplierDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId.Value;
        var codeExists = await _dbContext.Suppliers
            .AnyAsync(s => s.Code == dto.Code && s.TenantId == tenantId && s.StoreId == storeId && !s.IsDeleted);
        if (codeExists)
            return ApiResponseDto<SupplierDto>.Fail($"供应商编码 {dto.Code} 在当前门店已存在", 400);

        var entity = dto.Adapt<SupplierEntity>();
        entity.TenantId = tenantId;
        entity.TenantCode = _currentUser.TenantCode ?? string.Empty;
        entity.StoreId = storeId;
        entity.StoreCode = _currentUser.StoreCode ?? string.Empty;
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
        if (!_currentUser.TenantId.HasValue || !_currentUser.StoreId.HasValue)
            return ApiResponseDto<SupplierDto>.Fail("无法确定当前门店", 401);

        var validation = await _updateValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<SupplierDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId.Value;
        var entity = await _dbContext.Suppliers
            .FirstOrDefaultAsync(s => s.Id == dto.Id && !s.IsDeleted
                && s.TenantId == tenantId && s.StoreId == storeId);
        if (entity == null)
            return ApiResponseDto<SupplierDto>.Fail("供应商不存在", 404);

        // 编码变更时检查唯一性（门店内）
        if (entity.Code != dto.Code)
        {
            var codeExists = await _dbContext.Suppliers
                .AnyAsync(s => s.Code == dto.Code && s.TenantId == tenantId && s.StoreId == storeId && !s.IsDeleted && s.Id != dto.Id);
            if (codeExists)
                return ApiResponseDto<SupplierDto>.Fail($"供应商编码 {dto.Code} 在当前门店已存在", 400);
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
    /// 级联清理：物理删除该供应商在当前门店的所有 ProductSupplier 关联；
    /// 若被删关联中含默认供应商，自动将剩余关联中最早创建的设为新默认
    /// </summary>
    public async Task<ApiResponseDto> DeleteAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue || !_currentUser.StoreId.HasValue)
            return ApiResponseDto.Fail("无法确定当前门店", 401);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId.Value;
        var entity = await _dbContext.Suppliers
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted
                && s.TenantId == tenantId
                && s.StoreId == storeId);
        if (entity == null)
            return ApiResponseDto.Fail("供应商不存在", 404);

        entity.IsDeleted = true;
        entity.UpdatedTime = DateTime.Now;

        // 级联清理该供应商的所有 ProductSupplier 关联，并迁移受影响商品的默认供应商
        await CleanupSupplierRelationsAsync(new List<long> { id }, tenantId, storeId);

        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, "删除成功");
    }

    /// <summary>
    /// 批量删除供应商（软删除）
    /// 级联清理：物理删除这些供应商的所有 ProductSupplier 关联，并迁移受影响商品的默认供应商
    /// </summary>
    public async Task<ApiResponseDto> BatchDeleteAsync(List<long> ids)
    {
        if (!_currentUser.TenantId.HasValue || !_currentUser.StoreId.HasValue)
            return ApiResponseDto.Fail("无法确定当前门店", 401);
        if (ids == null || !ids.Any())
            return ApiResponseDto.Fail("请选择要删除的数据", 400);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId.Value;
        var entities = await _dbContext.Suppliers
            .Where(s => ids.Contains(s.Id) && !s.IsDeleted
                && s.TenantId == tenantId
                && s.StoreId == storeId)
            .ToListAsync();

        foreach (var entity in entities)
        {
            entity.IsDeleted = true;
            entity.UpdatedTime = DateTime.Now;
        }

        // 级联清理这些供应商的所有 ProductSupplier 关联，并迁移受影响商品的默认供应商
        var deletedSupplierIds = entities.Select(s => s.Id).ToList();
        await CleanupSupplierRelationsAsync(deletedSupplierIds, tenantId, storeId);

        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, $"成功删除 {entities.Count} 条数据");
    }

    /// <summary>
    /// 级联清理供应商-品项关联并迁移默认供应商
    /// 1. 查询这些供应商在当前门店的所有 ProductSupplier 关联
    /// 2. 物理删除这些关联
    /// 3. 对每个受影响商品：若被删关联中含默认供应商，查剩余关联（按 CreatedTime 升序）取首个设为新默认
    /// </summary>
    private async Task CleanupSupplierRelationsAsync(List<long> supplierIds, long tenantId, long storeId)
    {
        if (supplierIds == null || !supplierIds.Any()) return;

        // 一次性查询所有受影响的 ProductSupplier 关联
        var relationsToRemove = await _dbContext.ProductSuppliers
            .Where(ps => supplierIds.Contains(ps.SupplierId)
                && ps.TenantId == tenantId
                && ps.StoreId == storeId)
            .ToListAsync();
        if (!relationsToRemove.Any()) return;

        // 被删除默认供应商的商品需要迁移默认
        var productsNeedMigration = relationsToRemove
            .Where(ps => ps.IsDefault)
            .Select(ps => ps.ProductId)
            .Distinct()
            .ToList();
        _dbContext.ProductSuppliers.RemoveRange(relationsToRemove);

        if (!productsNeedMigration.Any()) return;

        // 查询这些商品的剩余关联（排除本次删除的供应商），按 CreatedTime 升序取首个设为默认
        var remainingRelations = await _dbContext.ProductSuppliers
            .Where(ps => productsNeedMigration.Contains(ps.ProductId)
                && !supplierIds.Contains(ps.SupplierId)
                && ps.TenantId == tenantId
                && ps.StoreId == storeId)
            .ToListAsync();
        var now = DateTime.Now;

        foreach (var productId in productsNeedMigration)
        {
            var nextRelation = remainingRelations
                .Where(r => r.ProductId == productId)
                .OrderBy(r => r.CreatedTime)
                .FirstOrDefault();
            if (nextRelation != null)
            {
                nextRelation.IsDefault = true;
                nextRelation.UpdatedTime = now;
            }
        }
    }

    // ========== 供应商-品项关联（G2.6.2）==========

    /// <summary>
    /// 批量绑定品项到供应商
    /// 业务规则：已存在的关联跳过；品项无默认供应商时将首个绑定设为默认
    /// </summary>
    public async Task<ApiResponseDto<List<ProductSupplierDto>>> BindProductsAsync(BindProductsDto dto)
    {
        if (!_currentUser.TenantId.HasValue || !_currentUser.StoreId.HasValue)
            return ApiResponseDto<List<ProductSupplierDto>>.Fail("无法确定当前门店", 401);

        var validation = await _bindProductsValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<List<ProductSupplierDto>>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        if (dto.ProductIds == null || !dto.ProductIds.Any())
            return ApiResponseDto<List<ProductSupplierDto>>.Fail("请选择要绑定的品项", 400);

        var tenantId = _currentUser.TenantId.Value;
        var tenantCode = _currentUser.TenantCode ?? string.Empty;
        var storeId = _currentUser.StoreId.Value;
        var storeCode = _currentUser.StoreCode ?? string.Empty;
        var now = DateTime.Now;

        // 校验供应商存在（门店内）
        var supplierExists = await _dbContext.Suppliers
            .AnyAsync(s => s.Id == dto.SupplierId && !s.IsDeleted && s.TenantId == tenantId && s.StoreId == storeId);
        if (!supplierExists)
            return ApiResponseDto<List<ProductSupplierDto>>.Fail("供应商不存在", 404);

        // 校验所有品项存在（去重后查询，门店内）
        var productIds = dto.ProductIds.Distinct().ToList();
        var existingProductIds = await _dbContext.Products
            .Where(p => productIds.Contains(p.Id) && !p.IsDeleted && p.TenantId == tenantId && p.StoreId == storeId)
            .Select(p => p.Id)
            .ToListAsync();
        var invalidProductIds = productIds.Except(existingProductIds).ToList();
        if (invalidProductIds.Any())
            return ApiResponseDto<List<ProductSupplierDto>>.Fail($"以下品项不存在：{string.Join(",", invalidProductIds)}", 404);

        // 查询已存在的关联（避免重复绑定，门店内）
        var existingRelations = await _dbContext.ProductSuppliers
            .Where(ps => ps.SupplierId == dto.SupplierId
                && productIds.Contains(ps.ProductId)
                && ps.TenantId == tenantId
                && ps.StoreId == storeId)
            .ToListAsync();
        var existingProductIdsBound = existingRelations.Select(ps => ps.ProductId).ToHashSet();

        var toCreate = productIds.Except(existingProductIdsBound).ToList();
        var createdEntities = new List<ProductSupplier>();
        foreach (var productId in toCreate)
        {
            // 品项当前是否已有默认供应商（决定本次绑定是否设为默认，门店内）
            var hasDefault = await _dbContext.ProductSuppliers
                .AnyAsync(ps => ps.ProductId == productId && ps.IsDefault && ps.TenantId == tenantId && ps.StoreId == storeId);
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
                StoreId = storeId,
                StoreCode = storeCode,
                CreatedTime = now
            };
            _dbContext.ProductSuppliers.Add(relation);
            createdEntities.Add(relation);
        }

        if (createdEntities.Any())
            await _dbContext.SaveChangesAsync();

        // 返回本次绑定的关联记录（含品项编码/名称），按门店隔离查询
        var boundProductIds = createdEntities.Select(ps => ps.ProductId).ToList();
        var productsInfo = await _dbContext.Products
            .Where(p => boundProductIds.Contains(p.Id) && p.TenantId == tenantId && p.StoreId == storeId)
            .Select(p => new { p.Id, p.Code, p.Name })
            .ToListAsync();
        var supplierInfo = await _dbContext.Suppliers
            .Where(s => s.Id == dto.SupplierId && s.TenantId == tenantId && s.StoreId == storeId)
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
    /// 业务规则：物理删除关联记录；若解除的是默认供应商，自动将下一个关联设为新默认
    /// </summary>
    public async Task<ApiResponseDto> UnbindProductAsync(long productId, long supplierId)
    {
        if (!_currentUser.TenantId.HasValue || !_currentUser.StoreId.HasValue)
            return ApiResponseDto.Fail("无法确定当前门店", 401);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId.Value;
        var relation = await _dbContext.ProductSuppliers
            .FirstOrDefaultAsync(ps => ps.ProductId == productId
                && ps.SupplierId == supplierId
                && ps.TenantId == tenantId
                && ps.StoreId == storeId);
        if (relation == null)
            return ApiResponseDto.Fail("品项与供应商未建立关联", 404);

        var wasDefault = relation.IsDefault;
        _dbContext.ProductSuppliers.Remove(relation);

        // 解除默认供应商时，自动选择下一个关联作为新默认
        if (wasDefault)
        {
            var nextRelation = await _dbContext.ProductSuppliers
                .Where(ps => ps.ProductId == productId && ps.TenantId == tenantId && ps.StoreId == storeId && ps.SupplierId != supplierId)
                .OrderBy(ps => ps.CreatedTime)
                .FirstOrDefaultAsync();
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
    /// 业务规则：自动取消旧默认；可选更新参考价与供货周期
    /// </summary>
    public async Task<ApiResponseDto<ProductSupplierDto>> SetDefaultSupplierAsync(SetDefaultSupplierDto dto)
    {
        if (!_currentUser.TenantId.HasValue || !_currentUser.StoreId.HasValue)
            return ApiResponseDto<ProductSupplierDto>.Fail("无法确定当前门店", 401);

        var validation = await _setDefaultSupplierValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<ProductSupplierDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId.Value;
        var relation = await _dbContext.ProductSuppliers
            .FirstOrDefaultAsync(ps => ps.ProductId == dto.ProductId
                && ps.SupplierId == dto.SupplierId
                && ps.TenantId == tenantId
                && ps.StoreId == storeId);
        if (relation == null)
            return ApiResponseDto<ProductSupplierDto>.Fail("品项与供应商未建立关联，请先绑定", 404);

        var now = DateTime.Now;

        // 取消旧默认（一个品项仅一个默认供应商，门店内）
        var oldDefaults = await _dbContext.ProductSuppliers
            .Where(ps => ps.ProductId == dto.ProductId && ps.IsDefault && ps.SupplierId != dto.SupplierId && ps.TenantId == tenantId && ps.StoreId == storeId)
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

        await _dbContext.SaveChangesAsync();

        // 组装返回 DTO（含品项与供应商编码/名称），按门店隔离查询
        var productInfo = await _dbContext.Products
            .Where(p => p.Id == dto.ProductId && p.TenantId == tenantId && p.StoreId == storeId)
            .Select(p => new { p.Code, p.Name })
            .FirstOrDefaultAsync();
        var supplierInfo = await _dbContext.Suppliers
            .Where(s => s.Id == dto.SupplierId && s.TenantId == tenantId && s.StoreId == storeId)
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
        if (!_currentUser.TenantId.HasValue || !_currentUser.StoreId.HasValue)
            return ApiResponseDto<List<ProductSupplierDto>>.Fail("无法确定当前门店", 401);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId.Value;
        var supplierExists = await _dbContext.Suppliers
            .AnyAsync(s => s.Id == supplierId && !s.IsDeleted && s.TenantId == tenantId && s.StoreId == storeId);
        if (!supplierExists)
            return ApiResponseDto<List<ProductSupplierDto>>.Fail("供应商不存在", 404);

        var relations = await _dbContext.ProductSuppliers
            .Where(ps => ps.SupplierId == supplierId && ps.TenantId == tenantId && ps.StoreId == storeId)
            .OrderByDescending(ps => ps.IsDefault)
            .ThenByDescending(ps => ps.CreatedTime)
            .ToListAsync();
        if (!relations.Any())
            return ApiResponseDto<List<ProductSupplierDto>>.Ok(new List<ProductSupplierDto>());

        var productIds = relations.Select(ps => ps.ProductId).ToList();
        // 过滤已软删除品项，避免展示无效关联；按门店隔离查询
        var productsInfo = await _dbContext.Products
            .Where(p => productIds.Contains(p.Id) && p.TenantId == tenantId && p.StoreId == storeId && !p.IsDeleted)
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

    /// <summary>
    /// 获取供应商轻量选项列表（不分页，仅返回 Id/Name）
    /// 仅按 TenantId + StoreId 过滤，排除已软删除供应商
    /// </summary>
    public async Task<ApiResponseDto<List<SupplierOptionDto>>> GetOptionsAsync()
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<List<SupplierOptionDto>>.Fail("无法确定当前租户", 401);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId ?? 0;

        var options = await _dbContext.Suppliers
            .Where(s => !s.IsDeleted && s.TenantId == tenantId && s.StoreId == storeId)
            .OrderBy(s => s.Name)
            .Select(s => new SupplierOptionDto
            {
                Id = s.Id,
                Name = s.Name
            })
            .ToListAsync();

        return ApiResponseDto<List<SupplierOptionDto>>.Ok(options);
    }
}
