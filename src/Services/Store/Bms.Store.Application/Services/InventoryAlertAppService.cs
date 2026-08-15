using Mapster;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Bms.BuildingBlocks.Abstractions.Security;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Inventories;
using InventoryAlertEntity = Bms.Store.Domain.Entities.InventoryAlert;
using Bms.Store.Domain.Entities;
using Bms.Store.Infrastructure;

namespace Bms.Store.Application.Services;

/// <summary>
/// 库存预警应用服务实现
/// </summary>
public class InventoryAlertAppService : IInventoryAlertAppService
{
    private readonly StoreDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<InventoryAlertCreateDto> _createValidator;
    private readonly IValidator<InventoryAlertUpdateDto> _updateValidator;

    public InventoryAlertAppService(
        StoreDbContext dbContext,
        ICurrentUser currentUser,
        IValidator<InventoryAlertCreateDto> createValidator,
        IValidator<InventoryAlertUpdateDto> updateValidator)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>
    /// 获取库存预警分页列表（联表商品主档/分类/门店，支持按商品名称模糊查询）
    /// </summary>
    public async Task<ApiResponseDto<PagedResponseDto<InventoryAlertDto>>> GetPagedListAsync(InventoryAlertQueryDto query)
    {
        if (!_currentUser.TenantId.HasValue || !_currentUser.StoreId.HasValue)
            return ApiResponseDto<PagedResponseDto<InventoryAlertDto>>.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId.Value;

        // join Products -> ProductMaster + Stores 获取显示字段
        // 左连接 InventoryBatches 取 BatchNo（仅效期预警有值）
        var queryable = from alert in _dbContext.InventoryAlerts
                        join prod in _dbContext.Products on alert.ProductId equals prod.Id
                        join master in _dbContext.ProductMasters on prod.MasterId equals master.Id
                        join store in _dbContext.Stores on alert.StoreId equals store.Id
                        join batch in _dbContext.InventoryBatches on alert.BatchId equals batch.Id into batchGroup
                        from batch in batchGroup.DefaultIfEmpty()
                        where alert.TenantId == tenantId && alert.StoreId == storeId
                              && prod.Status == 1 // 排除下架商品
                        select new
                        {
                            alert,
                            ProductName = master.Name,
                            ProductCode = master.Code,
                            StoreName = store.Name,
                            BatchNo = batch != null ? batch.BatchNo : null
                        };

        if (query.ProductId.HasValue)
            queryable = queryable.Where(x => x.alert.ProductId == query.ProductId.Value);
        if (query.AlertType.HasValue)
            queryable = queryable.Where(x => x.alert.AlertType == query.AlertType.Value);
        if (query.IsProcessed.HasValue)
            queryable = queryable.Where(x => x.alert.IsProcessed == query.IsProcessed.Value);
        if (!string.IsNullOrWhiteSpace(query.ProductName))
            queryable = queryable.Where(x => x.ProductName.Contains(query.ProductName));

        var total = await queryable.CountAsync();
        var rows = await queryable
            .OrderByDescending(x => x.alert.CreatedTime)
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var list = rows.Select(x => new InventoryAlertDto
        {
            Id = x.alert.Id,
            ProductId = x.alert.ProductId,
            AlertType = x.alert.AlertType,
            CurrentQuantity = x.alert.CurrentQuantity,
            AlertValue = x.alert.AlertValue,
            ExpirationDate = x.alert.ExpirationDate,
            BatchId = x.alert.BatchId,
            IsProcessed = x.alert.IsProcessed,
            ProcessedTime = x.alert.ProcessedTime,
            ProcessedRemark = x.alert.ProcessedRemark,
            CreatedAt = x.alert.CreatedTime,
            UpdatedAt = x.alert.UpdatedTime,
            ProductName = x.ProductName,
            ProductCode = x.ProductCode,
            BatchNo = x.BatchNo,
            StoreName = x.StoreName,
            ShortageAmount = x.alert.AlertType == 1 ? x.alert.AlertValue - x.alert.CurrentQuantity : 0
        }).ToList();

        var result = new PagedResponseDto<InventoryAlertDto>
        {
            List = list,
            Total = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
        return ApiResponseDto<PagedResponseDto<InventoryAlertDto>>.Ok(result);
    }

    /// <summary>
    /// 根据ID获取库存预警详情
    /// </summary>
    public async Task<ApiResponseDto<InventoryAlertDto?>> GetByIdAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue || !_currentUser.StoreId.HasValue)
            return ApiResponseDto<InventoryAlertDto?>.Fail("登录状态异常，请重新登录", 401);

        var entity = await _dbContext.InventoryAlerts
            .FirstOrDefaultAsync(a => a.Id == id && a.TenantId == _currentUser.TenantId.Value && a.StoreId == _currentUser.StoreId.Value);
        if (entity == null)
            return ApiResponseDto<InventoryAlertDto?>.Fail("库存预警不存在", 404);
        return ApiResponseDto<InventoryAlertDto?>.Ok(entity.Adapt<InventoryAlertDto>());
    }

    /// <summary>
    /// 创建库存预警
    /// </summary>
    public async Task<ApiResponseDto<InventoryAlertDto>> CreateAsync(InventoryAlertCreateDto dto)
    {
        if (!_currentUser.TenantId.HasValue || !_currentUser.StoreId.HasValue)
            return ApiResponseDto<InventoryAlertDto>.Fail("登录状态异常，请重新登录", 401);

        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<InventoryAlertDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId.Value;
        var entity = dto.Adapt<InventoryAlertEntity>();
        entity.TenantId = tenantId;
        entity.TenantCode = _currentUser.TenantCode ?? string.Empty;
        entity.StoreId = storeId;
        entity.CreatedTime = DateTime.Now;

        _dbContext.InventoryAlerts.Add(entity);
        await _dbContext.SaveChangesAsync();

        return ApiResponseDto<InventoryAlertDto>.Ok(entity.Adapt<InventoryAlertDto>(), "创建成功");
    }

    /// <summary>
    /// 更新库存预警
    /// </summary>
    public async Task<ApiResponseDto<InventoryAlertDto>> UpdateAsync(InventoryAlertUpdateDto dto)
    {
        if (!_currentUser.TenantId.HasValue || !_currentUser.StoreId.HasValue)
            return ApiResponseDto<InventoryAlertDto>.Fail("登录状态异常，请重新登录", 401);

        var validation = await _updateValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<InventoryAlertDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId.Value;
        var entity = await _dbContext.InventoryAlerts
            .FirstOrDefaultAsync(a => a.Id == dto.Id && a.TenantId == tenantId && a.StoreId == storeId);
        if (entity == null)
            return ApiResponseDto<InventoryAlertDto>.Fail("库存预警不存在", 404);

        entity.ProductId = dto.ProductId;
        entity.AlertType = dto.AlertType;
        entity.CurrentQuantity = dto.CurrentQuantity;
        entity.AlertValue = dto.AlertValue;
        entity.ExpirationDate = dto.ExpirationDate;
        entity.IsProcessed = dto.IsProcessed;
        entity.ProcessedTime = dto.ProcessedTime;
        entity.ProcessedRemark = dto.ProcessedRemark;
        entity.UpdatedTime = DateTime.Now;

        await _dbContext.SaveChangesAsync();
        return ApiResponseDto<InventoryAlertDto>.Ok(entity.Adapt<InventoryAlertDto>(), "更新成功");
    }

    /// <summary>
    /// 删除库存预警（物理删除）
    /// </summary>
    public async Task<ApiResponseDto> DeleteAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue || !_currentUser.StoreId.HasValue)
            return ApiResponseDto.Fail("登录状态异常，请重新登录", 401);

        var entity = await _dbContext.InventoryAlerts
            .FirstOrDefaultAsync(a => a.Id == id && a.TenantId == _currentUser.TenantId.Value && a.StoreId == _currentUser.StoreId.Value);
        if (entity == null)
            return ApiResponseDto.Fail("库存预警不存在", 404);

        _dbContext.InventoryAlerts.Remove(entity);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, "删除成功");
    }

    /// <summary>
    /// 批量删除库存预警（物理删除）
    /// </summary>
    public async Task<ApiResponseDto> BatchDeleteAsync(List<long> ids)
    {
        if (!_currentUser.TenantId.HasValue || !_currentUser.StoreId.HasValue)
            return ApiResponseDto.Fail("登录状态异常，请重新登录", 401);
        if (ids == null || !ids.Any())
            return ApiResponseDto.Fail("请选择要删除的数据", 400);

        var entities = await _dbContext.InventoryAlerts
            .Where(a => ids.Contains(a.Id) && a.TenantId == _currentUser.TenantId.Value && a.StoreId == _currentUser.StoreId.Value)
            .ToListAsync();

        _dbContext.InventoryAlerts.RemoveRange(entities);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, $"成功删除 {entities.Count} 条数据");
    }

    // ============================================================
    // 预警扫描与处理
    // ============================================================

    /// <summary>
    /// 全量扫描并生成预警（低库存/效期/积压），返回新生成预警数量
    /// </summary>
    public async Task<ApiResponseDto<InventoryAlertScanResultDto>> ScanAsync()
    {
        var result = new InventoryAlertScanResultDto();
        var now = DateTime.Now;
        var today = now.Date;

        // 1. 低库存预警扫描
        result.LowStockCreated = await ScanLowStockAsync(now);

        // 2. 效期预警扫描（含过期批次状态更新）
        var (expiryCreated, batchExpired) = await ScanExpiryAsync(now, today);
        result.ExpiryCreated = expiryCreated;
        result.BatchExpired = batchExpired;

        // 3. 积压预警扫描
        result.OverstockCreated = await ScanOverstockAsync(now);

        return ApiResponseDto<InventoryAlertScanResultDto>.Ok(result,
            $"扫描完成：低库存 {result.LowStockCreated} 条、效期 {result.ExpiryCreated} 条、积压 {result.OverstockCreated} 条、过期批次 {result.BatchExpired} 个");
    }

    /// <summary>
    /// 即时检测指定商品的预警（库存变动后调用）
    /// 双向处理低库存/积压预警的生成与关闭，并关闭已用完批次的效期预警
    /// </summary>
    public async Task CheckInventoryAlertsAsync(long tenantId, long storeId, long productId)
    {
        var now = DateTime.Now;

        // 查询商品（含阈值与租户/门店编码）
        var product = await _dbContext.Products
            .Where(p => p.Id == productId && p.TenantId == tenantId && !p.IsDeleted)
            .Select(p => new { p.LowStockThreshold, p.OverstockThreshold, p.TenantCode, p.StoreCode })
            .FirstOrDefaultAsync();
        if (product == null) return;

        // 查询当前库存，无 Inventory 记录视为 0（与库存管理页面状态判定一致）
        var inventory = await _dbContext.Inventories
            .FirstOrDefaultAsync(i => i.TenantId == tenantId && i.StoreId == storeId && i.ProductId == productId);
        var currentQuantity = inventory?.Quantity ?? 0m;

        // ===== 低库存预警：双向处理 =====
        if (product.LowStockThreshold.HasValue)
        {
            var threshold = product.LowStockThreshold.Value;
            if (currentQuantity < threshold)
            {
                // 库存低于阈值：若无未处理预警则生成
                var exists = await _dbContext.InventoryAlerts
                    .AnyAsync(a => a.TenantId == tenantId && a.StoreId == storeId
                                && a.ProductId == productId && a.AlertType == 1 && !a.IsProcessed);
                if (!exists)
                {
                    _dbContext.InventoryAlerts.Add(new InventoryAlertEntity
                    {
                        TenantId = tenantId,
                        TenantCode = product.TenantCode,
                        StoreId = storeId,
                        StoreCode = product.StoreCode,
                        ProductId = productId,
                        AlertType = 1,
                        CurrentQuantity = currentQuantity,
                        AlertValue = threshold,
                        IsProcessed = false,
                        CreatedTime = now
                    });
                }
            }
            else
            {
                // 库存已恢复：关闭未处理低库存预警
                await CloseAlertsAsync(tenantId, storeId, productId, 1, "系统自动关闭（库存已恢复）", now);
            }
        }

        // ===== 积压预警：双向处理 =====
        if (product.OverstockThreshold.HasValue)
        {
            var threshold = product.OverstockThreshold.Value;
            if (currentQuantity > threshold)
            {
                // 库存超过阈值：若无未处理预警则生成
                var exists = await _dbContext.InventoryAlerts
                    .AnyAsync(a => a.TenantId == tenantId && a.StoreId == storeId
                                && a.ProductId == productId && a.AlertType == 3 && !a.IsProcessed);
                if (!exists)
                {
                    _dbContext.InventoryAlerts.Add(new InventoryAlertEntity
                    {
                        TenantId = tenantId,
                        TenantCode = product.TenantCode,
                        StoreId = storeId,
                        StoreCode = product.StoreCode,
                        ProductId = productId,
                        AlertType = 3,
                        CurrentQuantity = currentQuantity,
                        AlertValue = threshold,
                        IsProcessed = false,
                        CreatedTime = now
                    });
                }
            }
            else
            {
                // 库存已恢复：关闭未处理积压预警
                await CloseAlertsAsync(tenantId, storeId, productId, 3, "系统自动关闭（库存已恢复）", now);
            }
        }

        // ===== 效期预警：关闭已用完批次对应预警 =====
        // 效期预警的生成由定时扫描 ScanExpiryAsync 负责（含近效期+已过期），此处仅关闭
        var usedUpBatchIds = await _dbContext.InventoryBatches
            .Where(b => b.TenantId == tenantId && b.StoreId == storeId && b.ProductId == productId
                    && (b.Status == 2 || (b.Status == 3 && b.Quantity == 0)))
            .Select(b => (long?)b.Id)
            .ToListAsync();
        if (usedUpBatchIds.Any())
        {
            await CloseExpiryAlertsAsync(tenantId, storeId, productId, usedUpBatchIds, "系统自动关闭（批次已用完）", now);
        }

        await _dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// 关闭指定商品+预警类型的未处理预警（通用方法）
    /// </summary>
    private async Task CloseAlertsAsync(long tenantId, long storeId, long productId, int alertType, string remark, DateTime now)
    {
        var alerts = await _dbContext.InventoryAlerts
            .Where(a => a.TenantId == tenantId && a.StoreId == storeId
                    && a.ProductId == productId && a.AlertType == alertType && !a.IsProcessed)
            .ToListAsync();
        foreach (var alert in alerts)
        {
            alert.IsProcessed = true;
            alert.ProcessedTime = now;
            alert.ProcessedRemark = remark;
            alert.UpdatedTime = now;
        }
    }

    /// <summary>
    /// 关闭指定商品+批次的未处理效期预警
    /// </summary>
    private async Task CloseExpiryAlertsAsync(long tenantId, long storeId, long productId, List<long?> batchIds, string remark, DateTime now)
    {
        var alerts = await _dbContext.InventoryAlerts
            .Where(a => a.TenantId == tenantId && a.StoreId == storeId
                    && a.ProductId == productId && a.AlertType == 2 && !a.IsProcessed
                    && a.BatchId.HasValue && batchIds.Contains(a.BatchId.Value))
            .ToListAsync();
        foreach (var alert in alerts)
        {
            alert.IsProcessed = true;
            alert.ProcessedTime = now;
            alert.ProcessedRemark = remark;
            alert.UpdatedTime = now;
        }
    }

    // ============================================================
    // 私有扫描方法
    // ============================================================

    /// <summary>
    /// 低库存预警扫描
    /// 以 Product 为主表左连接 Inventory，使无库存记录的商品（quantity=0）也能被扫描到，
    /// 与 InventoryAppService.GetPagedListAsync 的查询结构保持一致
    /// </summary>
    private async Task<int> ScanLowStockAsync(DateTime now)
    {
        // 查询库存低于阈值的商品（阈值非空），正品/样品/赠品统一处理
        // 无 Inventory 记录的商品视为 quantity=0，与库存管理页面状态判定逻辑一致
        var candidates = await (
            from p in _dbContext.Products
            join i in _dbContext.Inventories
                on new { ProductId = p.Id, p.TenantId, p.StoreId }
                equals new { ProductId = i.ProductId, i.TenantId, i.StoreId }
                into inventories
            from i in inventories.DefaultIfEmpty()
            where p.LowStockThreshold != null
                  && !p.IsDeleted
                  && p.Status == 1 // 排除下架商品
                  && (i == null ? 0m : i.Quantity) < p.LowStockThreshold
            select new
            {
                p.TenantId,
                p.TenantCode,
                p.StoreId,
                p.StoreCode,
                ProductId = p.Id,
                Quantity = i == null ? 0m : i.Quantity,
                Threshold = p.LowStockThreshold!.Value
            }
        ).ToListAsync();

        if (candidates.Count == 0)
            return 0;

        // 查询已存在的未处理低库存预警，用于去重
        var existingKeys = await _dbContext.InventoryAlerts
            .Where(a => a.AlertType == 1 && !a.IsProcessed)
            .Select(a => new { a.StoreId, a.ProductId })
            .ToListAsync();
        var existingSet = existingKeys.Select(k => (k.StoreId, k.ProductId)).ToHashSet();

        var created = 0;
        foreach (var c in candidates)
        {
            if (existingSet.Contains((c.StoreId, c.ProductId)))
                continue;

            _dbContext.InventoryAlerts.Add(new InventoryAlertEntity
            {
                TenantId = c.TenantId,
                TenantCode = c.TenantCode,
                StoreId = c.StoreId,
                StoreCode = c.StoreCode,
                ProductId = c.ProductId,
                AlertType = 1,
                CurrentQuantity = c.Quantity,
                AlertValue = c.Threshold,
                IsProcessed = false,
                CreatedTime = now
            });
            created++;
        }

        if (created > 0)
            await _dbContext.SaveChangesAsync();

        // 关闭已恢复的未处理低库存预警（库存 >= 阈值）
        var resolvedAlerts = await (
            from a in _dbContext.InventoryAlerts
            join p in _dbContext.Products on a.ProductId equals p.Id
            join i in _dbContext.Inventories
                on new { ProductId = p.Id, p.TenantId, p.StoreId }
                equals new { ProductId = i.ProductId, i.TenantId, i.StoreId }
                into inventories
            from i in inventories.DefaultIfEmpty()
            where a.AlertType == 1 && !a.IsProcessed
                  && p.LowStockThreshold != null
                  && !p.IsDeleted
                  && (i == null ? 0m : i.Quantity) >= p.LowStockThreshold
            select a
        ).ToListAsync();

        foreach (var alert in resolvedAlerts)
        {
            alert.IsProcessed = true;
            alert.ProcessedTime = now;
            alert.ProcessedRemark = "系统自动关闭（库存已恢复）";
            alert.UpdatedTime = now;
        }

        if (resolvedAlerts.Count > 0)
            await _dbContext.SaveChangesAsync();

        return created;
    }

    /// <summary>
    /// 效期预警扫描（含过期批次状态更新）
    /// </summary>
    private async Task<(int created, int batchExpired)> ScanExpiryAsync(DateTime now, DateTime today)
    {
        // 1. 更新已过期批次状态（ExpirationDate < Today 且 Status=1）
        var expiredBatches = await _dbContext.InventoryBatches
            .Where(b => b.Status == 1 && b.ExpirationDate != null && b.ExpirationDate < today)
            .ToListAsync();

        foreach (var batch in expiredBatches)
        {
            batch.Status = 3; // 已过期
            batch.UpdatedTime = now;
        }

        if (expiredBatches.Count > 0)
            await _dbContext.SaveChangesAsync();

        // 2. 查询近效期批次（Status=1 且剩余天数 <= 阈值）和已过期批次（Status=3 且 Quantity > 0）
        // 已过期批次也需生成预警，确保门店能看到过期商品（需销毁处理）
        var candidates = await (
            from batch in _dbContext.InventoryBatches
            join prod in _dbContext.Products on batch.ProductId equals prod.Id
            where !prod.IsDeleted
                  && prod.Status == 1 // 排除下架商品
                  && batch.ExpirationDate != null
                  && (
                      // 近效期：Status=1 且剩余天数 <= ExpiryAlertDays
                      (batch.Status == 1 && prod.ExpiryAlertDays != null
                       && batch.ExpirationDate.Value <= today.AddDays(prod.ExpiryAlertDays!.Value))
                      // 已过期且有库存：Status=3 且 Quantity > 0
                      || (batch.Status == 3 && batch.Quantity > 0)
                  )
            select new
            {
                batch.TenantId,
                batch.TenantCode,
                batch.StoreId,
                batch.StoreCode,
                batch.ProductId,
                BatchId = (long?)batch.Id,
                batch.BatchNo,
                batch.ExpirationDate
            }
        ).ToListAsync();

        // 去重：同一门店+商品+批次+效期预警类型且未处理的预警已存在则跳过
        var existingKeys = await _dbContext.InventoryAlerts
            .Where(a => a.AlertType == 2 && !a.IsProcessed)
            .Select(a => new { a.StoreId, a.ProductId, a.BatchId })
            .ToListAsync();
        var existingSet = existingKeys.Select(k => (k.StoreId, k.ProductId, k.BatchId)).ToHashSet();

        var created = 0;
        foreach (var c in candidates)
        {
            if (existingSet.Contains((c.StoreId, c.ProductId, c.BatchId)))
                continue;

            var remainingDays = (int)(c.ExpirationDate!.Value.Date - today).TotalDays;

            _dbContext.InventoryAlerts.Add(new InventoryAlertEntity
            {
                TenantId = c.TenantId,
                TenantCode = c.TenantCode,
                StoreId = c.StoreId,
                StoreCode = c.StoreCode,
                ProductId = c.ProductId,
                AlertType = 2,
                CurrentQuantity = 0,
                AlertValue = remainingDays,
                ExpirationDate = c.ExpirationDate,
                BatchId = c.BatchId,
                IsProcessed = false,
                CreatedTime = now
            });
            created++;
        }

        if (created > 0)
            await _dbContext.SaveChangesAsync();

        // 3. 关闭已用完批次（Status=2 或 Status=3 且 Qty=0）的未处理效期预警
        var usedUpBatchIds = await _dbContext.InventoryBatches
            .Where(b => b.Status == 2 || (b.Status == 3 && b.Quantity == 0))
            .Select(b => (long?)b.Id)
            .ToListAsync();

        if (usedUpBatchIds.Any())
        {
            var resolvedAlerts = await _dbContext.InventoryAlerts
                .Where(a => a.AlertType == 2 && !a.IsProcessed
                        && a.BatchId.HasValue && usedUpBatchIds.Contains(a.BatchId.Value))
                .ToListAsync();

            foreach (var alert in resolvedAlerts)
            {
                alert.IsProcessed = true;
                alert.ProcessedTime = now;
                alert.ProcessedRemark = "系统自动关闭（批次已用完）";
                alert.UpdatedTime = now;
            }

            if (resolvedAlerts.Count > 0)
                await _dbContext.SaveChangesAsync();
        }

        return (created, expiredBatches.Count);
    }

    /// <summary>
    /// 积压预警扫描
    /// 以 Product 为主表左连接 Inventory，与 ScanLowStockAsync 查询结构保持一致
    /// </summary>
    private async Task<int> ScanOverstockAsync(DateTime now)
    {
        // 查询库存超过积压阈值的商品（阈值非空），正品/样品/赠品统一处理
        // 无 Inventory 记录的商品视为 quantity=0，不会触发积压预警，逻辑等价于 INNER JOIN
        var candidates = await (
            from p in _dbContext.Products
            join i in _dbContext.Inventories
                on new { ProductId = p.Id, p.TenantId, p.StoreId }
                equals new { ProductId = i.ProductId, i.TenantId, i.StoreId }
                into inventories
            from i in inventories.DefaultIfEmpty()
            where p.OverstockThreshold != null
                  && !p.IsDeleted
                  && p.Status == 1 // 排除下架商品
                  && (i == null ? 0m : i.Quantity) > p.OverstockThreshold
            select new
            {
                p.TenantId,
                p.TenantCode,
                p.StoreId,
                p.StoreCode,
                ProductId = p.Id,
                Quantity = i == null ? 0m : i.Quantity,
                Threshold = p.OverstockThreshold!.Value
            }
        ).ToListAsync();

        if (candidates.Count == 0)
            return 0;

        // 去重：同一门店+商品+积压类型且未处理的预警已存在则跳过
        var existingKeys = await _dbContext.InventoryAlerts
            .Where(a => a.AlertType == 3 && !a.IsProcessed)
            .Select(a => new { a.StoreId, a.ProductId })
            .ToListAsync();
        var existingSet = existingKeys.Select(k => (k.StoreId, k.ProductId)).ToHashSet();

        var created = 0;
        foreach (var c in candidates)
        {
            if (existingSet.Contains((c.StoreId, c.ProductId)))
                continue;

            _dbContext.InventoryAlerts.Add(new InventoryAlertEntity
            {
                TenantId = c.TenantId,
                TenantCode = c.TenantCode,
                StoreId = c.StoreId,
                StoreCode = c.StoreCode,
                ProductId = c.ProductId,
                AlertType = 3,
                CurrentQuantity = c.Quantity,
                AlertValue = c.Threshold,
                IsProcessed = false,
                CreatedTime = now
            });
            created++;
        }

        if (created > 0)
            await _dbContext.SaveChangesAsync();

        // 关闭已恢复的未处理积压预警（库存 <= 阈值）
        var resolvedAlerts = await (
            from a in _dbContext.InventoryAlerts
            join p in _dbContext.Products on a.ProductId equals p.Id
            join i in _dbContext.Inventories
                on new { ProductId = p.Id, p.TenantId, p.StoreId }
                equals new { ProductId = i.ProductId, i.TenantId, i.StoreId }
                into inventories
            from i in inventories.DefaultIfEmpty()
            where a.AlertType == 3 && !a.IsProcessed
                  && p.OverstockThreshold != null
                  && !p.IsDeleted
                  && (i == null ? 0m : i.Quantity) <= p.OverstockThreshold
            select a
        ).ToListAsync();

        foreach (var alert in resolvedAlerts)
        {
            alert.IsProcessed = true;
            alert.ProcessedTime = now;
            alert.ProcessedRemark = "系统自动关闭（库存已恢复）";
            alert.UpdatedTime = now;
        }

        if (resolvedAlerts.Count > 0)
            await _dbContext.SaveChangesAsync();

        return created;
    }
}
