using Mapster;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Bms.BuildingBlocks.Abstractions.Security;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Products;
using Bms.Store.Domain.Entities;
using Bms.Store.Infrastructure;

namespace Bms.Store.Application.Services;

/// <summary>
/// 商品主档应用服务实现（租户级，承载商品本质属性）
/// 对应设计文档 6.1/6.2/7.2/8.1 节
/// Master 字段全租户生效，Store 字段通过 BatchConfigStoreFieldsAsync 统一配置
/// </summary>
public class ProductMasterAppService : IProductMasterAppService
{
    private readonly StoreDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<ProductMasterCreateDto> _createValidator;
    private readonly IValidator<ProductMasterUpdateDto> _updateValidator;
    private readonly IValidator<ProductStoreBatchConfigDto> _batchConfigValidator;

    public ProductMasterAppService(
        StoreDbContext dbContext,
        ICurrentUser currentUser,
        IValidator<ProductMasterCreateDto> createValidator,
        IValidator<ProductMasterUpdateDto> updateValidator,
        IValidator<ProductStoreBatchConfigDto> batchConfigValidator)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _batchConfigValidator = batchConfigValidator;
    }

    /// <summary>
    /// 获取商品主档分页列表（租户级，含子表字段）
    /// </summary>
    public async Task<ApiResponseDto<PagedResponseDto<ProductMasterDto>>> GetPagedListAsync(ProductMasterQueryDto query)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<PagedResponseDto<ProductMasterDto>>.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;
        var queryable = _dbContext.ProductMasters
            .Include(m => m.Category)
            .Where(m => !m.IsDeleted && m.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(query.Name))
            queryable = queryable.Where(m => m.Name.Contains(query.Name));
        if (!string.IsNullOrWhiteSpace(query.Code))
            queryable = queryable.Where(m => m.Code.Contains(query.Code));
        if (query.Type.HasValue)
            queryable = queryable.Where(m => m.Type == query.Type.Value);
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
            queryable = queryable.Where(m => categoryIds.Contains(m.CategoryId));
        }

        var total = await queryable.CountAsync();
        var items = await queryable
            .OrderByDescending(m => m.CreatedTime)
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var dtoList = items.Adapt<List<ProductMasterDto>>();
        await FillSubTableFieldsAsync(dtoList, tenantId);

        return ApiResponseDto<PagedResponseDto<ProductMasterDto>>.Ok(new PagedResponseDto<ProductMasterDto>
        {
            List = dtoList,
            Total = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        });
    }

    /// <summary>
    /// 根据ID获取商品主档详情（含子表字段）
    /// </summary>
    public async Task<ApiResponseDto<ProductMasterDto?>> GetByIdAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<ProductMasterDto?>.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;
        var master = await _dbContext.ProductMasters
            .Include(m => m.Category)
            .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted && m.TenantId == tenantId);
        if (master == null)
            return ApiResponseDto<ProductMasterDto?>.Fail("商品主档不存在", 404);

        var dto = master.Adapt<ProductMasterDto>();
        await FillSubTableFieldsForMasterAsync(dto, master.Id, tenantId);
        return ApiResponseDto<ProductMasterDto?>.Ok(dto);
    }

    /// <summary>
    /// 创建商品主档（服务商品 Type=2 同时创建 ServiceProduct 子表）
    /// </summary>
    public async Task<ApiResponseDto<ProductMasterDto>> CreateAsync(ProductMasterCreateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<ProductMasterDto>.Fail("登录状态异常，请重新登录", 401);

        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<ProductMasterDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;

        // Code 租户内唯一校验
        var codeExists = await _dbContext.ProductMasters
            .AnyAsync(m => m.Code == dto.Code && m.TenantId == tenantId && !m.IsDeleted);
        if (codeExists)
            return ApiResponseDto<ProductMasterDto>.Fail($"商品编码 {dto.Code} 已存在", 400);

        var master = new ProductMaster
        {
            Code = dto.Code,
            Name = dto.Name,
            Type = dto.Type,
            CategoryId = dto.CategoryId,
            Unit = dto.Unit,
            Specification = dto.Specification,
            Brand = dto.Brand,
            ImageUrl = dto.ImageUrl,
            Remark = dto.Remark,
            // 样品/赠品（Type=4/5）强制不可销售（设计文档 4.1 节 IsSalable 跟 Type 走）
            IsSalable = dto.Type != 4 && dto.Type != 5,
            TenantId = tenantId,
            TenantCode = _currentUser.TenantCode ?? string.Empty,
            CreatedTime = DateTime.Now
        };
        _dbContext.ProductMasters.Add(master);
        await _dbContext.SaveChangesAsync();

        // 服务商品（Type=2）创建 ServiceProduct 子表
        if (dto.Type == 2)
            await CreateServiceProductSubTableAsync(dto, master.Id, tenantId);

        await _dbContext.Entry(master).Reference(m => m.Category).LoadAsync();
        var dtoResult = master.Adapt<ProductMasterDto>();
        await FillSubTableFieldsForMasterAsync(dtoResult, master.Id, tenantId);
        return ApiResponseDto<ProductMasterDto>.Ok(dtoResult, "创建成功");
    }

    /// <summary>
    /// 更新商品主档（Master 字段全租户生效，含子表类型变更处理）
    /// </summary>
    public async Task<ApiResponseDto<ProductMasterDto>> UpdateAsync(ProductMasterUpdateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<ProductMasterDto>.Fail("登录状态异常，请重新登录", 401);

        var validation = await _updateValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<ProductMasterDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var master = await _dbContext.ProductMasters
            .FirstOrDefaultAsync(m => m.Id == dto.Id && !m.IsDeleted && m.TenantId == tenantId);
        if (master == null)
            return ApiResponseDto<ProductMasterDto>.Fail("商品主档不存在", 404);

        // Code 变更检查唯一性
        if (master.Code != dto.Code)
        {
            var codeExists = await _dbContext.ProductMasters
                .AnyAsync(m => m.Code == dto.Code && m.TenantId == tenantId && !m.IsDeleted && m.Id != dto.Id);
            if (codeExists)
                return ApiResponseDto<ProductMasterDto>.Fail($"商品编码 {dto.Code} 已存在", 400);
        }

        var oldType = master.Type;
        // Master 字段全租户生效（设计文档 6.2 节）
        master.Code = dto.Code;
        master.Name = dto.Name;
        master.Type = dto.Type;
        master.CategoryId = dto.CategoryId;
        master.Unit = dto.Unit;
        master.Specification = dto.Specification;
        master.Brand = dto.Brand;
        master.ImageUrl = dto.ImageUrl;
        master.Remark = dto.Remark;
        master.IsSalable = dto.Type != 4 && dto.Type != 5;
        master.UpdatedTime = DateTime.Now;

        await _dbContext.SaveChangesAsync();

        // 类型变更处理 ServiceProduct 子表
        if (oldType != dto.Type)
        {
            await SoftDeleteServiceProductAsync(dto.Id, tenantId);
            if (dto.Type == 2)
            {
                var createDto = new ProductMasterCreateDto
                {
                    Duration = dto.Duration,
                    RequiredRoomType = dto.RequiredRoomType,
                    EquipmentTypeIds = dto.EquipmentTypeIds,
                    SkillCategoryIds = dto.SkillCategoryIds
                };
                await CreateServiceProductSubTableAsync(createDto, dto.Id, tenantId);
            }
        }
        else if (dto.Type == 2)
        {
            await UpdateServiceProductAsync(dto, dto.Id, tenantId);
        }

        await _dbContext.Entry(master).Reference(m => m.Category).LoadAsync();
        var dtoResult = master.Adapt<ProductMasterDto>();
        await FillSubTableFieldsForMasterAsync(dtoResult, dto.Id, tenantId);
        return ApiResponseDto<ProductMasterDto>.Ok(dtoResult, "更新成功");
    }

    /// <summary>
    /// 删除商品主档（设计文档 8.1 节：保守策略）
    /// 任何关联门店有库存 > 0 时禁止删除；全部为 0 时事务内级联软删除 Master + 关联 Product + ServiceProduct 子表
    /// </summary>
    public async Task<ApiResponseDto> DeleteAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;
        var master = await _dbContext.ProductMasters
            .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted && m.TenantId == tenantId);
        if (master == null)
            return ApiResponseDto.Fail("商品主档不存在", 404);

        // 查询所有关联 Product（门店档案）
        var relatedProducts = await _dbContext.Products
            .Where(p => p.MasterId == id && !p.IsDeleted && p.TenantId == tenantId)
            .ToListAsync();

        // 库存检查：任何门店 Inventory.Quantity > 0 禁止删除（设计文档 8.1 节）
        if (relatedProducts.Any())
        {
            var relatedProductIds = relatedProducts.Select(p => p.Id).ToList();
            var inStockStores = await (from inv in _dbContext.Inventories
                                       join p in _dbContext.Products on inv.ProductId equals p.Id
                                       join s in _dbContext.Stores on p.StoreId equals s.Id
                                       where relatedProductIds.Contains(inv.ProductId)
                                           && inv.TenantId == tenantId
                                           && inv.Quantity > 0
                                           && !s.IsDeleted
                                       select s.Name).Distinct().ToListAsync();

            if (inStockStores.Any())
                return ApiResponseDto.Fail($"以下门店仍有库存：[{string.Join("、", inStockStores)}]，请先清空后再删除", 400);
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            var now = DateTime.Now;

            // 软删除 Master
            master.IsDeleted = true;
            master.UpdatedTime = now;

            // 软删除所有关联 Product（门店档案）
            foreach (var product in relatedProducts)
            {
                product.IsDeleted = true;
                product.UpdatedTime = now;
            }

            // 软删除 ServiceProduct 子表 + 设备类型关联（设计文档 8.1 节：级联软删除子表）
            await SoftDeleteServiceProductAsync(id, tenantId);

            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
            return ApiResponseDto.Success(null, "删除成功");
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// 统一配置门店档案 Store 字段（对应设计文档 7.2/7.3 节）
    /// 将 Store 字段值应用到选中门店：已有 Product -> 覆盖；无 Product -> 自动创建
    /// 事务内执行（SaveChangesAsync 一次提交），避免部分失败导致数据不一致
    /// </summary>
    public async Task<ApiResponseDto> BatchConfigStoreFieldsAsync(ProductStoreBatchConfigDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("登录状态异常，请重新登录", 401);

        // Validator 已覆盖 StoreIds 非空、MasterId/Price/Status 等校验
        var validation = await _batchConfigValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var tenantCode = _currentUser.TenantCode ?? string.Empty;

        // 校验 Master 存在
        var masterExists = await _dbContext.ProductMasters
            .AnyAsync(m => m.Id == dto.MasterId && !m.IsDeleted && m.TenantId == tenantId);
        if (!masterExists)
            return ApiResponseDto.Fail("商品主档不存在", 404);

        // 校验选中门店均属于当前租户
        var validStores = await _dbContext.Stores
            .Where(s => dto.StoreIds.Contains(s.Id) && s.TenantId == tenantId && !s.IsDeleted)
            .Select(s => new { s.Id, s.Code })
            .ToListAsync();
        if (validStores.Count != dto.StoreIds.Count)
            return ApiResponseDto.Fail("部分门店不存在或不属于当前租户", 400);

        // 查询已有 Product 档案
        var existingProducts = await _dbContext.Products
            .Where(p => p.MasterId == dto.MasterId && p.TenantId == tenantId
                && dto.StoreIds.Contains(p.StoreId) && !p.IsDeleted)
            .ToListAsync();
        var existingStoreIds = existingProducts.Select(p => p.StoreId).ToHashSet();
        var now = DateTime.Now;

        // 覆盖已存在的 Product（设计文档 7.3 节：已有档案 -> 覆盖）
        // 价格变化时自动写入 PriceChangeLog（与 ProductAppService.UpdateAsync 行为一致）
        foreach (var product in existingProducts)
        {
            var oldPrice = product.Price;
            product.Price = dto.Price;
            product.CostPrice = dto.CostPrice;
            product.LowStockThreshold = dto.LowStockThreshold;
            product.ExpiryAlertDays = dto.ExpiryAlertDays;
            product.OverstockThreshold = dto.OverstockThreshold;
            product.Status = dto.Status;
            product.Remark = dto.Remark;
            product.UpdatedTime = now;

            // 价格变更记录（仅价格实际变化时写入；新建档案不写日志--首次定价非调价）
            if (oldPrice != dto.Price)
            {
                _dbContext.PriceChangeLogs.Add(new PriceChangeLog
                {
                    ProductId = product.Id,
                    OldPrice = oldPrice,
                    NewPrice = dto.Price,
                    ChangeTime = now,
                    OperatorId = _currentUser.UserId,
                    OperatorName = _currentUser.RealName ?? _currentUser.UserName,
                    Remark = "主档统一配置自动记录",
                    TenantId = tenantId,
                    CreatedTime = now
                });
            }
        }

        // 自动创建缺失的 Product（设计文档 7.3 节：无档案 -> 自动创建）
        var storesToCreate = validStores.Where(s => !existingStoreIds.Contains(s.Id));
        foreach (var store in storesToCreate)
        {
            _dbContext.Products.Add(new Product
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
                TenantCode = tenantCode,
                StoreId = store.Id,
                StoreCode = store.Code,
                CreatedTime = now
            });
        }

        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, $"已应用到 {dto.StoreIds.Count} 个门店");
    }

    /// <summary>
    /// 预览门店档案配置（对应设计文档 7.3 节二次确认）
    /// 查询选中门店中哪些已存在该主档的 Product 档案（将被覆盖），哪些无档案（将新建）
    /// 复用 BatchConfigStoreFieldsAsync 的存在性判断逻辑，确保预览结果与实际执行一致
    /// </summary>
    public async Task<ApiResponseDto<ProductStoreConfigPreviewDto>> GetStoreConfigPreviewAsync(long masterId, List<long> storeIds)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<ProductStoreConfigPreviewDto>.Fail("登录状态异常，请重新登录", 401);

        if (storeIds == null || storeIds.Count == 0)
            return ApiResponseDto<ProductStoreConfigPreviewDto>.Fail("请先选择至少一家门店", 400);

        var tenantId = _currentUser.TenantId.Value;

        // 校验 Master 存在
        var masterExists = await _dbContext.ProductMasters
            .AnyAsync(m => m.Id == masterId && !m.IsDeleted && m.TenantId == tenantId);
        if (!masterExists)
            return ApiResponseDto<ProductStoreConfigPreviewDto>.Fail("商品主档不存在", 404);

        // 校验选中门店均属于当前租户
        var validStoreCount = await _dbContext.Stores
            .CountAsync(s => storeIds.Contains(s.Id) && s.TenantId == tenantId && !s.IsDeleted);
        if (validStoreCount != storeIds.Count)
            return ApiResponseDto<ProductStoreConfigPreviewDto>.Fail("部分门店不存在或不属于当前租户", 400);

        // 查询已有 Product 档案的门店ID（与 BatchConfigStoreFieldsAsync 判断逻辑一致）
        var existingStoreIds = await _dbContext.Products
            .Where(p => p.MasterId == masterId && p.TenantId == tenantId
                && storeIds.Contains(p.StoreId) && !p.IsDeleted)
            .Select(p => p.StoreId)
            .ToListAsync();

        return ApiResponseDto<ProductStoreConfigPreviewDto>.Ok(new ProductStoreConfigPreviewDto
        {
            ExistingStoreIds = existingStoreIds
        });
    }

    /// <summary>
    /// 获取商品主档轻量选项列表（下拉选择用）
    /// </summary>
    public async Task<ApiResponseDto<List<ProductMasterOptionDto>>> GetOptionsAsync()
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<List<ProductMasterOptionDto>>.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;
        var options = await _dbContext.ProductMasters
            .Where(m => !m.IsDeleted && m.TenantId == tenantId)
            .OrderBy(m => m.Name)
            .Select(m => new ProductMasterOptionDto
            {
                Id = m.Id,
                Code = m.Code,
                Name = m.Name,
                Type = m.Type,
                Unit = m.Unit
            })
            .ToListAsync();

        return ApiResponseDto<List<ProductMasterOptionDto>>.Ok(options);
    }

    // ========== 子表辅助方法 ==========

    /// <summary>
    /// 批量填充服务项目子表字段到 Master DTO 列表
    /// </summary>
    private async Task FillSubTableFieldsAsync(List<ProductMasterDto> dtos, long tenantId)
    {
        if (!dtos.Any()) return;

        var type2Ids = dtos.Where(d => d.Type == 2).Select(d => d.Id).ToList();
        if (!type2Ids.Any()) return;

        var services = await _dbContext.ServiceProducts
            .Where(s => type2Ids.Contains(s.MasterId) && !s.IsDeleted && s.TenantId == tenantId)
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
            if (serviceDict.TryGetValue(dto.Id, out var s))
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
    /// 填充单个 Master 的服务项目子表字段
    /// </summary>
    private async Task FillSubTableFieldsForMasterAsync(ProductMasterDto dto, long masterId, long tenantId)
    {
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

    /// <summary>
    /// 创建 ServiceProduct 子表 + 设备类型关联 + 适用技能关联
    /// </summary>
    private async Task CreateServiceProductSubTableAsync(ProductMasterCreateDto dto, long masterId, long tenantId)
    {
        var tenantCode = _currentUser.TenantCode ?? string.Empty;
        var storeId = _currentUser.StoreId ?? 0;
        var storeCode = _currentUser.StoreCode ?? string.Empty;
        var service = new ServiceProduct
        {
            MasterId = masterId,
            Duration = dto.Duration,
            RequiredRoomType = dto.RequiredRoomType,
            TenantId = tenantId,
            TenantCode = tenantCode,
            CreatedTime = DateTime.Now
        };
        _dbContext.ServiceProducts.Add(service);
        await _dbContext.SaveChangesAsync();

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
                    // ServiceProductEquipment 跟随 ServiceProduct 归 Master 层（租户级），
                    // StoreId/StoreCode 字段冗余，待后续统一清理为 StoreTenantEntity
                    StoreId = 0,
                    StoreCode = string.Empty,
                    CreatedTime = DateTime.Now
                });
            }
            await _dbContext.SaveChangesAsync();
        }

        // 适用技能关联（门店语境：ServiceProductSkill 按当前门店隔离，跨店各门店独立配置）
        if (dto.SkillCategoryIds != null && dto.SkillCategoryIds.Any())
        {
            foreach (var skillCategoryId in dto.SkillCategoryIds.Distinct())
            {
                _dbContext.ServiceProductSkills.Add(new ServiceProductSkill
                {
                    ServiceProductId = service.Id,
                    SkillCategoryId = skillCategoryId,
                    TenantId = tenantId,
                    TenantCode = tenantCode,
                    StoreId = storeId,
                    StoreCode = storeCode,
                    CreatedTime = DateTime.Now
                });
            }
            await _dbContext.SaveChangesAsync();
        }
    }

    /// <summary>
    /// 更新 ServiceProduct 子表（类型不变场景）
    /// </summary>
    private async Task UpdateServiceProductAsync(ProductMasterUpdateDto dto, long masterId, long tenantId)
    {
        var service = await _dbContext.ServiceProducts
            .FirstOrDefaultAsync(s => s.MasterId == masterId && !s.IsDeleted && s.TenantId == tenantId);
        if (service == null)
        {
            // 子表不存在则创建
            var createDto = new ProductMasterCreateDto
            {
                Duration = dto.Duration,
                RequiredRoomType = dto.RequiredRoomType,
                EquipmentTypeIds = dto.EquipmentTypeIds,
                SkillCategoryIds = dto.SkillCategoryIds
            };
            await CreateServiceProductSubTableAsync(createDto, masterId, tenantId);
            return;
        }

        service.Duration = dto.Duration;
        service.RequiredRoomType = dto.RequiredRoomType;
        service.UpdatedTime = DateTime.Now;

        // 替换设备类型关联：物理删除旧记录 + 创建新记录（关联表不启用软删除）
        var oldRelations = await _dbContext.ServiceProductEquipments
            .Where(r => r.ServiceProductId == service.Id)
            .ToListAsync();
        _dbContext.ServiceProductEquipments.RemoveRange(oldRelations);

        if (dto.EquipmentTypeIds != null && dto.EquipmentTypeIds.Any())
        {
            var tenantCode = _currentUser.TenantCode ?? string.Empty;
            foreach (var equipmentTypeId in dto.EquipmentTypeIds.Distinct())
            {
                _dbContext.ServiceProductEquipments.Add(new ServiceProductEquipment
                {
                    ServiceProductId = service.Id,
                    EquipmentTypeId = equipmentTypeId,
                    TenantId = tenantId,
                    TenantCode = tenantCode,
                    // 同上：StoreId/StoreCode 冗余，待后续清理
                    StoreId = 0,
                    StoreCode = string.Empty,
                    CreatedTime = DateTime.Now
                });
            }
        }

        // 替换适用技能关联（按当前门店语境）：物理删除当前门店旧记录 + 创建新记录
        var storeId = _currentUser.StoreId ?? 0;
        var oldSkillRelations = await _dbContext.ServiceProductSkills
            .Where(r => r.ServiceProductId == service.Id && r.StoreId == storeId)
            .ToListAsync();
        _dbContext.ServiceProductSkills.RemoveRange(oldSkillRelations);

        if (dto.SkillCategoryIds != null && dto.SkillCategoryIds.Any())
        {
            var tenantCode = _currentUser.TenantCode ?? string.Empty;
            var storeCode = _currentUser.StoreCode ?? string.Empty;
            foreach (var skillCategoryId in dto.SkillCategoryIds.Distinct())
            {
                _dbContext.ServiceProductSkills.Add(new ServiceProductSkill
                {
                    ServiceProductId = service.Id,
                    SkillCategoryId = skillCategoryId,
                    TenantId = tenantId,
                    TenantCode = tenantCode,
                    StoreId = storeId,
                    StoreCode = storeCode,
                    CreatedTime = DateTime.Now
                });
            }
        }
        await _dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// 软删除 ServiceProduct 子表 + 设备类型关联（类型变更场景）
    /// </summary>
    private async Task SoftDeleteServiceProductAsync(long masterId, long tenantId)
    {
        var service = await _dbContext.ServiceProducts
            .FirstOrDefaultAsync(s => s.MasterId == masterId && !s.IsDeleted && s.TenantId == tenantId);
        if (service == null) return;

        service.IsDeleted = true;
        service.UpdatedTime = DateTime.Now;

        var relations = await _dbContext.ServiceProductEquipments
            .Where(r => r.ServiceProductId == service.Id)
            .ToListAsync();
        _dbContext.ServiceProductEquipments.RemoveRange(relations);

        // 级联物理删除适用技能关联
        var skillRelations = await _dbContext.ServiceProductSkills
            .Where(r => r.ServiceProductId == service.Id)
            .ToListAsync();
        _dbContext.ServiceProductSkills.RemoveRange(skillRelations);
        await _dbContext.SaveChangesAsync();
    }
}
