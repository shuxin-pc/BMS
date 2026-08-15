using Mapster;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Bms.BuildingBlocks.Abstractions.Security;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Inventories;
using Bms.Store.Domain.Entities;
using InventoryEntity = Bms.Store.Domain.Entities.Inventory;
using Bms.Store.Infrastructure;

namespace Bms.Store.Application.Services;

/// <summary>
/// 库存应用服务实现
/// </summary>
public class InventoryAppService : IInventoryAppService
{
    private readonly StoreDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<InventoryCreateDto> _createValidator;
    private readonly IValidator<InventoryUpdateDto> _updateValidator;

    public InventoryAppService(
        StoreDbContext dbContext,
        ICurrentUser currentUser,
        IValidator<InventoryCreateDto> createValidator,
        IValidator<InventoryUpdateDto> updateValidator)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>
    /// 获取库存分页列表（按当前门店隔离）
    /// 以档案为主表左连接库存汇总表，档案存在即可见，无库存记录时数量为 0
    /// 联表 ProductMaster/ProductCategory 输出展示字段，并支持按类型、分类、商品名称、库存状态筛选
    /// </summary>
    public async Task<ApiResponseDto<PagedResponseDto<InventoryDto>>> GetPagedListAsync(InventoryQueryDto query)
    {
        if (!_currentUser.TenantId.HasValue || !_currentUser.StoreId.HasValue)
            return ApiResponseDto<PagedResponseDto<InventoryDto>>.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId.Value;
        // 以 Product 档案为主表，左连接 Inventory 汇总表，确保档案存在即可见
        // Name/Code/Type/CategoryId 已移至 ProductMaster，通过 Master 关联（设计文档 3.2 节）
        var queryable = from p in _dbContext.Products
                        join m in _dbContext.ProductMasters on p.MasterId equals m.Id
                        join c in _dbContext.ProductCategories on m.CategoryId equals c.Id into cgrp
                        from c in cgrp.DefaultIfEmpty()
                        join i in _dbContext.Inventories
                            on new { ProductId = p.Id, TenantId = tenantId, StoreId = storeId }
                            equals new { ProductId = i.ProductId, TenantId = i.TenantId, StoreId = i.StoreId }
                            into inventories
                        from i in inventories.DefaultIfEmpty()
                        where !p.IsDeleted && p.TenantId == tenantId && p.StoreId == storeId && m.Type != 2 // 排除服务项目（无实物库存）
                        select new { p, m, i, CategoryName = (c != null ? c.Name : null) };

        if (query.ProductId.HasValue)
            queryable = queryable.Where(x => x.p.Id == query.ProductId.Value);
        if (query.ProductType.HasValue)
            queryable = queryable.Where(x => x.m.Type == query.ProductType.Value);
        if (!string.IsNullOrWhiteSpace(query.ProductName))
            queryable = queryable.Where(x => x.m.Name.Contains(query.ProductName));
        if (query.CategoryId.HasValue)
        {
            // 选中父级分类时包含其所有子孙分类下的商品：
            // 先加载当前租户全部分类到内存（分类数据量小），BFS 收集子树 ID 集合，再用 Contains 筛选
            // 分类已改租户级，不再按 StoreId 过滤（设计文档 3.3 节）
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
            queryable = queryable.Where(x => categoryIds.Contains(x.m.CategoryId));
        }

        var total = await queryable.CountAsync();
        var rawItems = await queryable
            .OrderByDescending(x => x.p.CreatedTime)
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        // 库存状态需结合阈值在内存中计算，InventoryStatus 筛选在映射后过滤
        var dtoList = rawItems.Select(x => new InventoryDto
        {
            Id = x.p.Id,
            ProductId = x.p.Id,
            Quantity = x.i?.Quantity ?? 0,
            AlertQuantity = x.p.LowStockThreshold,
            OverstockThreshold = x.p.OverstockThreshold,
            InventoryStatus = CalculateInventoryStatus(x.i?.Quantity ?? 0, x.p.LowStockThreshold, x.p.OverstockThreshold),
            CreatedAt = x.p.CreatedTime,
            UpdatedAt = x.i?.UpdatedTime ?? x.p.UpdatedTime ?? x.p.CreatedTime,
            ProductName = x.m.Name,
            ProductCode = x.m.Code,
            ProductType = x.m.Type,
            CategoryName = x.CategoryName
        }).ToList();

        if (query.InventoryStatus.HasValue)
            dtoList = dtoList.Where(d => d.InventoryStatus == query.InventoryStatus.Value).ToList();

        var result = new PagedResponseDto<InventoryDto>
        {
            List = dtoList,
            Total = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
        return ApiResponseDto<PagedResponseDto<InventoryDto>>.Ok(result);
    }

    /// <summary>
    /// 根据档案ID获取库存详情
    /// 以档案为主表左连接库存汇总表，档案存在即可见
    /// </summary>
    public async Task<ApiResponseDto<InventoryDto?>> GetByIdAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue || !_currentUser.StoreId.HasValue)
            return ApiResponseDto<InventoryDto?>.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId.Value;
        var row = await (from p in _dbContext.Products
                         join m in _dbContext.ProductMasters on p.MasterId equals m.Id
                         join c in _dbContext.ProductCategories on m.CategoryId equals c.Id into cgrp
                         from c in cgrp.DefaultIfEmpty()
                         join i in _dbContext.Inventories
                             on new { ProductId = p.Id, TenantId = tenantId, StoreId = storeId }
                             equals new { ProductId = i.ProductId, TenantId = i.TenantId, StoreId = i.StoreId }
                             into inventories
                         from i in inventories.DefaultIfEmpty()
                         where p.Id == id && p.TenantId == tenantId && p.StoreId == storeId && !p.IsDeleted
                         select new { p, m, i, CategoryName = (c != null ? c.Name : null) })
                         .FirstOrDefaultAsync();

        if (row == null)
            return ApiResponseDto<InventoryDto?>.Fail("库存不存在", 404);

        var dto = new InventoryDto
        {
            Id = row.p.Id,
            ProductId = row.p.Id,
            Quantity = row.i?.Quantity ?? 0,
            AlertQuantity = row.p.LowStockThreshold,
            OverstockThreshold = row.p.OverstockThreshold,
            InventoryStatus = CalculateInventoryStatus(row.i?.Quantity ?? 0, row.p.LowStockThreshold, row.p.OverstockThreshold),
            CreatedAt = row.p.CreatedTime,
            UpdatedAt = row.i?.UpdatedTime ?? row.p.UpdatedTime ?? row.p.CreatedTime,
            ProductName = row.m.Name,
            ProductCode = row.m.Code,
            ProductType = row.m.Type,
            CategoryName = row.CategoryName
        };
        return ApiResponseDto<InventoryDto?>.Ok(dto);
    }

    /// <summary>
    /// 创建库存
    /// </summary>
    public async Task<ApiResponseDto<InventoryDto>> CreateAsync(InventoryCreateDto dto)
    {
        if (!_currentUser.TenantId.HasValue || !_currentUser.StoreId.HasValue)
            return ApiResponseDto<InventoryDto>.Fail("登录状态异常，请重新登录", 401);

        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<InventoryDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId.Value;
        var entity = dto.Adapt<InventoryEntity>();
        entity.TenantId = tenantId;
        entity.TenantCode = _currentUser.TenantCode ?? string.Empty;
        entity.StoreId = storeId;
        entity.CreatedTime = DateTime.Now;

        _dbContext.Inventories.Add(entity);
        await _dbContext.SaveChangesAsync();

        return ApiResponseDto<InventoryDto>.Ok(entity.Adapt<InventoryDto>(), "创建成功");
    }

    /// <summary>
    /// 更新库存
    /// </summary>
    public async Task<ApiResponseDto<InventoryDto>> UpdateAsync(InventoryUpdateDto dto)
    {
        if (!_currentUser.TenantId.HasValue || !_currentUser.StoreId.HasValue)
            return ApiResponseDto<InventoryDto>.Fail("登录状态异常，请重新登录", 401);

        var validation = await _updateValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<InventoryDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId.Value;
        var entity = await _dbContext.Inventories
            .FirstOrDefaultAsync(i => i.Id == dto.Id && i.TenantId == tenantId && i.StoreId == storeId);
        if (entity == null)
            return ApiResponseDto<InventoryDto>.Fail("库存不存在", 404);

        entity.ProductId = dto.ProductId;
        entity.Quantity = dto.Quantity;
        entity.UpdatedTime = DateTime.Now;

        await _dbContext.SaveChangesAsync();
        return ApiResponseDto<InventoryDto>.Ok(entity.Adapt<InventoryDto>(), "更新成功");
    }

    /// <summary>
    /// 删除库存（物理删除）
    /// </summary>
    public async Task<ApiResponseDto> DeleteAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue || !_currentUser.StoreId.HasValue)
            return ApiResponseDto.Fail("登录状态异常，请重新登录", 401);

        var entity = await _dbContext.Inventories
            .FirstOrDefaultAsync(i => i.Id == id && i.TenantId == _currentUser.TenantId.Value && i.StoreId == _currentUser.StoreId.Value);
        if (entity == null)
            return ApiResponseDto.Fail("库存不存在", 404);

        _dbContext.Inventories.Remove(entity);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, "删除成功");
    }

    /// <summary>
    /// 批量删除库存（物理删除）
    /// </summary>
    public async Task<ApiResponseDto> BatchDeleteAsync(List<long> ids)
    {
        if (!_currentUser.TenantId.HasValue || !_currentUser.StoreId.HasValue)
            return ApiResponseDto.Fail("登录状态异常，请重新登录", 401);
        if (ids == null || !ids.Any())
            return ApiResponseDto.Fail("请选择要删除的数据", 400);

        var entities = await _dbContext.Inventories
            .Where(i => ids.Contains(i.Id) && i.TenantId == _currentUser.TenantId.Value && i.StoreId == _currentUser.StoreId.Value)
            .ToListAsync();

        _dbContext.Inventories.RemoveRange(entities);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, $"成功删除 {entities.Count} 条数据");
    }

    /// <summary>
    /// 按批次扣减库存（用于采购退货等业务出库场景）
    /// 实现要点：
    /// 1. 指定 BatchNo 时扣减对应批次；未指定时按 FIFO（先进先出）扣减该品项的所有在库批次（Status=1）
    /// 2. 写入 InventoryLog（Type=2 出库，SourceType 由调用方传入）
    /// 3. 同步更新 Inventory 汇总表
    /// 4. 库存不足时返回 Fail，调用方应回滚事务
    /// 注意：本方法不调用 SaveChangesAsync 也不开启独立事务，调用方需在外层包裹事务并统一 SaveChanges
    /// </summary>
    public async Task<ApiResponseDto> DeductByBatchAsync(
        long productId,
        decimal quantity,
        string? batchNo,
        int sourceType,
        long? refId,
        string? remark = null)
    {
        if (!_currentUser.TenantId.HasValue || !_currentUser.StoreId.HasValue)
            return ApiResponseDto.Fail("登录状态异常，请重新登录", 401);
        if (quantity <= 0)
            return ApiResponseDto.Fail("扣减数量必须大于 0", 400);
        if (!InventoryLogSourceTypes.IsValid(sourceType))
            return ApiResponseDto.Fail($"来源类型 {sourceType} 不合法", 400);

        var tenantId = _currentUser.TenantId.Value;
        var tenantCode = _currentUser.TenantCode ?? string.Empty;
        var storeId = _currentUser.StoreId.Value;
        var now = DateTime.Now;

        // 定位待扣减的批次列表
        List<InventoryBatch> batches;
        if (!string.IsNullOrWhiteSpace(batchNo))
        {
            // 指定批次扣减
            batches = await _dbContext.InventoryBatches
                .Where(b => b.ProductId == productId
                    && b.BatchNo == batchNo
                    && b.TenantId == tenantId
                    && b.StoreId == storeId
                    && b.Status == 1)
                .ToListAsync();
            if (!batches.Any())
                return ApiResponseDto.Fail($"批次 {batchNo} 不存在或已用完", 404);
        }
        else
        {
            // FIFO 扣减：按 CreatedTime 升序取所有在库批次
            batches = await _dbContext.InventoryBatches
                .Where(b => b.ProductId == productId
                    && b.TenantId == tenantId
                    && b.StoreId == storeId
                    && b.Status == 1
                    && b.Quantity > 0)
                .OrderBy(b => b.CreatedTime)
                .ToListAsync();
            if (!batches.Any())
                return ApiResponseDto.Fail($"品项 {productId} 无可用库存批次", 404);
        }

        // 校验总库存是否充足
        var totalAvailable = batches.Sum(b => b.Quantity);
        if (totalAvailable < quantity)
            return ApiResponseDto.Fail($"库存不足：需要 {quantity}，可用 {totalAvailable}", 400);

        // 读取当前库存汇总（用于流水 BeforeQuantity/AfterQuantity）
        var inventory = await _dbContext.Inventories
            .FirstOrDefaultAsync(inv => inv.ProductId == productId && inv.TenantId == tenantId && inv.StoreId == storeId);
        var beforeQty = inventory?.Quantity ?? 0;

        var remaining = quantity;
        var firstBatchNo = batchNo;
        var firstExpiration = (DateTime?)null;
        var unitPrice = (decimal?)null;
        var firstLogged = false;

        foreach (var batch in batches)
        {
            if (remaining <= 0)
                break;

            var deduct = Math.Min(batch.Quantity, remaining);
            batch.Quantity -= deduct;
            batch.UpdatedTime = now;
            if (batch.Quantity == 0)
                batch.Status = 2; // 已用完

            remaining -= deduct;

            // 仅在第一条批次写入一条汇总流水（避免一条退货明细产生多条 InventoryLog）
            // 单价取首批次单价，关联批次号取首批次号，便于成本追溯
            if (!firstLogged)
            {
                firstBatchNo = batch.BatchNo;
                firstExpiration = batch.ExpirationDate;
                unitPrice = batch.UnitPrice;
                firstLogged = true;
            }
        }

        // 写入 InventoryLog（Type=2 出库）
        var log = new InventoryLog
        {
            ProductId = productId,
            Type = 2, // 出库
            SourceType = sourceType,
            UnitPrice = unitPrice,
            Quantity = -quantity, // 出库为负
            BeforeQuantity = beforeQty,
            AfterQuantity = beforeQty - quantity,
            BatchNo = firstBatchNo,
            ExpirationDate = firstExpiration,
            RelatedId = refId,
            Remark = remark,
            OperatorId = _currentUser.UserId,
            OperatorName = _currentUser.RealName ?? _currentUser.UserName,
            TenantId = tenantId,
            TenantCode = tenantCode,
            StoreId = storeId,
            CreatedTime = now
        };
        _dbContext.InventoryLogs.Add(log);

        // 同步扣减 Inventory 汇总表
        if (inventory != null)
        {
            inventory.Quantity -= quantity;
            inventory.UpdatedTime = now;
        }
        else
        {
            // 汇总表不存在但批次存在（异常数据）：补建汇总表，库存为负数表示数据不一致
            // 此分支理论上不应触发，因有批次必有汇总；防御性处理避免抛异常影响事务回滚
            inventory = new InventoryEntity
            {
                ProductId = productId,
                Quantity = -quantity,
                TenantId = tenantId,
                TenantCode = tenantCode,
                StoreId = storeId,
                CreatedTime = now
            };
            _dbContext.Inventories.Add(inventory);
        }

        return ApiResponseDto.Success(null, "扣减成功");
    }

    // ========== 辅助方法 ==========

    /// <summary>
    /// 计算库存状态：1=充足，2=偏低，3=不足，4=积压
    /// 优先级：不足 > 偏低 > 积压 > 充足（阈值配置异常导致同时满足时，紧急状态优先）
    /// 阈值未配置（null）时不参与对应状态判定，与 InventoryAlertAppService 预警扫描行为一致
    /// </summary>
    private static int CalculateInventoryStatus(decimal quantity, decimal? lowStockThreshold, decimal? overstockThreshold)
    {
        if (quantity <= 0)
            return 3; // 不足
        if (lowStockThreshold.HasValue && quantity <= lowStockThreshold.Value)
            return 2; // 偏低
        if (overstockThreshold.HasValue && quantity >= overstockThreshold.Value)
            return 4; // 积压
        return 1; // 充足
    }
}
