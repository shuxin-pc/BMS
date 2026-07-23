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
    /// 获取库存预警分页列表
    /// </summary>
    public async Task<ApiResponseDto<PagedResponseDto<InventoryAlertDto>>> GetPagedListAsync(InventoryAlertQueryDto query)
    {
        if (!_currentUser.TenantId.HasValue || !_currentUser.StoreId.HasValue)
            return ApiResponseDto<PagedResponseDto<InventoryAlertDto>>.Fail("无法确定当前租户或门店", 401);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId.Value;
        var queryable = _dbContext.InventoryAlerts
            .Where(a => a.TenantId == tenantId && a.StoreId == storeId);

        if (query.ProductId.HasValue)
            queryable = queryable.Where(a => a.ProductId == query.ProductId.Value);
        if (query.AlertType.HasValue)
            queryable = queryable.Where(a => a.AlertType == query.AlertType.Value);
        if (query.IsProcessed.HasValue)
            queryable = queryable.Where(a => a.IsProcessed == query.IsProcessed.Value);

        var total = await queryable.CountAsync();
        var items = await queryable
            .OrderByDescending(a => a.CreatedTime)
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var result = new PagedResponseDto<InventoryAlertDto>
        {
            List = items.Adapt<List<InventoryAlertDto>>(),
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
            return ApiResponseDto<InventoryAlertDto?>.Fail("无法确定当前租户或门店", 401);

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
            return ApiResponseDto<InventoryAlertDto>.Fail("无法确定当前租户或门店", 401);

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
            return ApiResponseDto<InventoryAlertDto>.Fail("无法确定当前租户或门店", 401);

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
            return ApiResponseDto.Fail("无法确定当前租户或门店", 401);

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
            return ApiResponseDto.Fail("无法确定当前租户或门店", 401);
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
    /// 即时检测指定商品的低库存预警（库存变动后调用）
    /// </summary>
    public async Task CheckLowStockAsync(long tenantId, long storeId, long productId)
    {
        var now = DateTime.Now;

        // 查询商品阈值
        var threshold = await _dbContext.Products
            .Where(p => p.Id == productId && p.TenantId == tenantId && !p.IsDeleted)
            .Select(p => p.LowStockThreshold)
            .FirstOrDefaultAsync();

        if (threshold == null)
            return; // 未设置阈值，不预警

        // 查询当前库存
        var inventory = await _dbContext.Inventories
            .FirstOrDefaultAsync(i => i.TenantId == tenantId && i.StoreId == storeId && i.ProductId == productId);

        if (inventory == null || inventory.Quantity >= threshold.Value)
            return; // 库存充足，不预警

        // 去重：同一门店+商品+低库存类型且未处理的预警已存在则跳过
        var exists = await _dbContext.InventoryAlerts
            .AnyAsync(a => a.TenantId == tenantId && a.StoreId == storeId
                        && a.ProductId == productId && a.AlertType == 1 && !a.IsProcessed);
        if (exists)
            return;

        var product = await _dbContext.Products
            .Where(p => p.Id == productId)
            .Select(p => new { p.TenantCode, p.StoreCode })
            .FirstOrDefaultAsync();

        _dbContext.InventoryAlerts.Add(new InventoryAlertEntity
        {
            TenantId = tenantId,
            TenantCode = product?.TenantCode ?? string.Empty,
            StoreId = storeId,
            StoreCode = product?.StoreCode ?? string.Empty,
            ProductId = productId,
            AlertType = 1,
            CurrentQuantity = inventory.Quantity,
            AlertValue = threshold.Value,
            IsProcessed = false,
            CreatedTime = now
        });

        await _dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// 标记预警已处理
    /// </summary>
    public async Task<ApiResponseDto> ProcessAsync(long id, string? remark)
    {
        if (!_currentUser.TenantId.HasValue || !_currentUser.StoreId.HasValue)
            return ApiResponseDto.Fail("无法确定当前租户或门店", 401);

        var entity = await _dbContext.InventoryAlerts
            .FirstOrDefaultAsync(a => a.Id == id
                && a.TenantId == _currentUser.TenantId.Value
                && a.StoreId == _currentUser.StoreId.Value);
        if (entity == null)
            return ApiResponseDto.Fail("库存预警不存在", 404);

        entity.IsProcessed = true;
        entity.ProcessedTime = DateTime.Now;
        entity.ProcessedRemark = remark;
        entity.UpdatedTime = DateTime.Now;

        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, "处理成功");
    }

    // ============================================================
    // 私有扫描方法
    // ============================================================

    /// <summary>
    /// 低库存预警扫描
    /// </summary>
    private async Task<int> ScanLowStockAsync(DateTime now)
    {
        // 查询库存低于阈值的商品（阈值非空），排除样品(4)/赠品(5)，它们由 SampleGiftAppService 独立管理
        var candidates = await (
            from inv in _dbContext.Inventories
            join prod in _dbContext.Products on inv.ProductId equals prod.Id
            where prod.LowStockThreshold != null
                  && inv.Quantity < prod.LowStockThreshold
                  && !prod.IsDeleted
                  && prod.Type != 4 && prod.Type != 5
            select new
            {
                inv.TenantId,
                inv.TenantCode,
                inv.StoreId,
                inv.StoreCode,
                inv.ProductId,
                inv.Quantity,
                Threshold = prod.LowStockThreshold!.Value
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

        // 2. 查询近效期批次（阈值非空且剩余天数 <= 阈值）
        var candidates = await (
            from batch in _dbContext.InventoryBatches
            join prod in _dbContext.Products on batch.ProductId equals prod.Id
            where batch.Status == 1
                  && batch.ExpirationDate != null
                  && prod.ExpiryAlertDays != null
                  && batch.ExpirationDate.Value <= today.AddDays(prod.ExpiryAlertDays!.Value)
                  && !prod.IsDeleted
            select new
            {
                batch.TenantId,
                batch.TenantCode,
                batch.StoreId,
                batch.StoreCode,
                batch.ProductId,
                batch.BatchNo,
                batch.ExpirationDate,
                ExpiryAlertDays = prod.ExpiryAlertDays!.Value
            }
        ).ToListAsync();

        if (candidates.Count == 0)
            return (0, expiredBatches.Count);

        // 去重：同一门店+商品+效期日期+效期预警类型且未处理的预警已存在则跳过
        var existingKeys = await _dbContext.InventoryAlerts
            .Where(a => a.AlertType == 2 && !a.IsProcessed)
            .Select(a => new { a.StoreId, a.ProductId, a.ExpirationDate })
            .ToListAsync();
        var existingSet = existingKeys.Select(k => (k.StoreId, k.ProductId, k.ExpirationDate)).ToHashSet();

        var created = 0;
        foreach (var c in candidates)
        {
            if (existingSet.Contains((c.StoreId, c.ProductId, c.ExpirationDate)))
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
                IsProcessed = false,
                CreatedTime = now
            });
            created++;
        }

        if (created > 0)
            await _dbContext.SaveChangesAsync();

        return (created, expiredBatches.Count);
    }

    /// <summary>
    /// 积压预警扫描
    /// </summary>
    private async Task<int> ScanOverstockAsync(DateTime now)
    {
        // 查询库存超过积压阈值的商品（阈值非空），排除样品(4)/赠品(5)，它们由 SampleGiftAppService 独立管理
        var candidates = await (
            from inv in _dbContext.Inventories
            join prod in _dbContext.Products on inv.ProductId equals prod.Id
            where prod.OverstockThreshold != null
                  && inv.Quantity > prod.OverstockThreshold
                  && !prod.IsDeleted
                  && prod.Type != 4 && prod.Type != 5
            select new
            {
                inv.TenantId,
                inv.TenantCode,
                inv.StoreId,
                inv.StoreCode,
                inv.ProductId,
                inv.Quantity,
                Threshold = prod.OverstockThreshold!.Value
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

        return created;
    }
}
