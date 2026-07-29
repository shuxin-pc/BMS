using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using FluentValidation;
using Bms.BuildingBlocks.Abstractions.Security;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Inventories;
using Bms.Store.Domain.Entities;
using Bms.Store.Infrastructure;

namespace Bms.Store.Application.Services;

/// <summary>
/// 出库应用服务实现
/// 事务内按 FEFO 顺序或用户指定批次扣减 InventoryBatch，每扣减一个批次写一条 InventoryLog，
/// 更新 Inventory 汇总表。支持两种模式：Quantity（FEFO 自动）/ BatchItems（手动指定）。
/// </summary>
public class OutboundAppService : IOutboundAppService
{
    private readonly StoreDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<OutboundCreateDto> _validator;
    private readonly ILogger<OutboundAppService> _logger;

    public OutboundAppService(
        StoreDbContext dbContext,
        ICurrentUser currentUser,
        IValidator<OutboundCreateDto> validator,
        ILogger<OutboundAppService> logger)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _validator = validator;
        _logger = logger;
    }

    /// <summary>
    /// 创建出库记录
    /// 事务边界：全程在 IDbContextTransaction 内执行，多表写入要么全部成功要么全部回滚
    /// </summary>
    public async Task<ApiResponseDto<OutboundResultDto>> CreateAsync(OutboundCreateDto dto)
    {
        if (!_currentUser.TenantId.HasValue || !_currentUser.StoreId.HasValue)
            return ApiResponseDto<OutboundResultDto>.Fail("无法确定当前租户或门店", 401);

        var validation = await _validator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<OutboundResultDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId.Value;

        // 本次出库产生的流水 ID 列表（用于 post-commit 重查询）
        var logIds = new List<long>();
        // 汇总数据（用于 post-commit fallback）
        decimal totalQuantity = 0;
        decimal beforeQuantity = 0;
        decimal afterQuantity = 0;

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            // 1. 校验商品存在且属于当前门店
            var product = await _dbContext.Products
                .FirstOrDefaultAsync(p => p.Id == dto.ProductId
                    && p.TenantId == tenantId
                    && p.StoreId == storeId
                    && !p.IsDeleted);
            if (product == null)
                return ApiResponseDto<OutboundResultDto>.Fail("商品不存在", 400);

            // 2. 查 Inventory 汇总表（防御性：记录不存在视为数据不一致，拒绝出库）
            var inventory = await _dbContext.Inventories
                .FirstOrDefaultAsync(i => i.ProductId == dto.ProductId
                    && i.TenantId == tenantId
                    && i.StoreId == storeId);
            if (inventory == null)
                return ApiResponseDto<OutboundResultDto>.Fail("库存不足", 400);

            beforeQuantity = inventory.Quantity;
            var currentInventoryQuantity = beforeQuantity;
            var operatorName = _currentUser.RealName ?? _currentUser.UserName;

            // 3. 判断模式并执行扣减
            if (dto.Quantity.HasValue)
            {
                // FEFO 自动模式
                var fefoResult = await DeductByFefoAsync(dto, tenantId, storeId, currentInventoryQuantity, operatorName);
                if (!fefoResult.Success)
                    return ApiResponseDto<OutboundResultDto>.Fail(fefoResult.ErrorMessage!, 400);
                logIds = fefoResult.LogIds!;
                totalQuantity = fefoResult.TotalQuantity;
                currentInventoryQuantity = fefoResult.AfterQuantity;
            }
            else
            {
                // 手动指定模式
                var manualResult = await DeductByManualAsync(dto, tenantId, storeId, currentInventoryQuantity, operatorName);
                if (!manualResult.Success)
                    return ApiResponseDto<OutboundResultDto>.Fail(manualResult.ErrorMessage!, 400);
                logIds = manualResult.LogIds!;
                totalQuantity = manualResult.TotalQuantity;
                currentInventoryQuantity = manualResult.AfterQuantity;
            }

            afterQuantity = currentInventoryQuantity;

            // 4. 更新 Inventory 汇总表
            inventory.Quantity = afterQuantity;
            inventory.UpdatedTime = DateTime.Now;

            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (DbUpdateException ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "出库失败：ProductId={ProductId}, SourceType={SourceType}", dto.ProductId, dto.SourceType);
            return ApiResponseDto<OutboundResultDto>.Fail("出库失败，请重试", 500);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "出库异常：ProductId={ProductId}, SourceType={SourceType}", dto.ProductId, dto.SourceType);
            return ApiResponseDto<OutboundResultDto>.Fail("出库失败，请重试", 500);
        }

        // 5. post-commit 重查询填充显示字段（移出 try 块：若此查询失败，数据已落库，
        // 不应回滚已提交事务或返回 500 让用户误重试导致重复出库）
        try
        {
            var logs = await _dbContext.InventoryLogs
                .Include(l => l.Product)
                .Include(l => l.Supplier)
                .Where(l => logIds.Contains(l.Id))
                .OrderBy(l => l.Id)
                .ToListAsync();

            var batchDetails = logs.Select(l => new InventoryLogDto
            {
                Id = l.Id,
                ProductId = l.ProductId,
                Type = l.Type,
                SourceType = l.SourceType,
                SupplierId = l.SupplierId,
                UnitPrice = l.UnitPrice,
                Quantity = l.Quantity,
                BeforeQuantity = l.BeforeQuantity,
                AfterQuantity = l.AfterQuantity,
                BatchNo = l.BatchNo,
                ExpirationDate = l.ExpirationDate,
                RelatedId = l.RelatedId,
                Remark = l.Remark,
                CreatedAt = l.CreatedTime,
                UpdatedAt = l.UpdatedTime,
                ProductName = l.Product?.Name,
                ProductCode = l.Product?.Code,
                SupplierName = l.Supplier?.Name,
                OperatorName = l.OperatorName
            }).ToList();

            var firstLog = logs.FirstOrDefault();
            var result = new OutboundResultDto
            {
                ProductId = dto.ProductId,
                ProductName = firstLog?.Product?.Name,
                ProductCode = firstLog?.Product?.Code,
                SourceType = dto.SourceType,
                TotalQuantity = totalQuantity,
                BeforeQuantity = beforeQuantity,
                AfterQuantity = afterQuantity,
                OperatorName = firstLog?.OperatorName,
                BatchDetails = batchDetails
            };
            return ApiResponseDto<OutboundResultDto>.Ok(result, "出库成功");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "出库已成功但显示字段查询失败：LogIds={LogIds}", string.Join(",", logIds));
            var fallback = new OutboundResultDto
            {
                ProductId = dto.ProductId,
                SourceType = dto.SourceType,
                TotalQuantity = totalQuantity,
                BeforeQuantity = beforeQuantity,
                AfterQuantity = afterQuantity,
                BatchDetails = new List<InventoryLogDto>()
            };
            return ApiResponseDto<OutboundResultDto>.Ok(fallback, "出库成功");
        }
    }

    /// <summary>
    /// FEFO 自动模式扣减：按 FEFO 顺序逐批次扣减
    /// </summary>
    private async Task<DeductResult> DeductByFefoAsync(
        OutboundCreateDto dto, long tenantId, long storeId,
        decimal currentInventoryQuantity, string operatorName)
    {
        var quantityToDeduct = dto.Quantity!.Value;

        // 查询所有在库批次按 FEFO 排序
        var batches = await _dbContext.InventoryBatches
            .Where(b => b.ProductId == dto.ProductId
                && b.TenantId == tenantId
                && b.StoreId == storeId
                && b.Status == 1
                && b.Quantity > 0)
            .OrderBy(b => b.ExpirationDate.HasValue ? 0 : 1)
            .ThenBy(b => b.ExpirationDate)
            .ThenBy(b => b.PurchaseDate)
            .ThenBy(b => b.CreatedTime)
            .ToListAsync();

        // 预检查：汇总总量
        var totalAvailable = batches.Sum(b => b.Quantity);
        if (totalAvailable < quantityToDeduct)
            return DeductResult.Fail($"库存不足，当前库存: {totalAvailable}");

        var logs = new List<InventoryLog>();
        var remaining = quantityToDeduct;

        foreach (var batch in batches)
        {
            if (remaining <= 0) break;

            var deductQty = Math.Min(remaining, batch.Quantity);

            // 写 InventoryLog（先收集，循环结束后统一 SaveChanges，与 InboundAppService 一致）
            var log = new InventoryLog
            {
                ProductId = dto.ProductId,
                Type = 2,
                SourceType = dto.SourceType,
                UnitPrice = dto.UnitPrice ?? batch.UnitPrice,
                Quantity = -deductQty,
                BeforeQuantity = currentInventoryQuantity,
                AfterQuantity = currentInventoryQuantity - deductQty,
                BatchNo = batch.BatchNo,
                ExpirationDate = batch.ExpirationDate,
                Remark = dto.Remark,
                OperatorName = operatorName,
                TenantId = tenantId,
                TenantCode = _currentUser.TenantCode ?? string.Empty,
                StoreId = storeId,
                StoreCode = _currentUser.StoreCode ?? string.Empty,
                CreatedTime = DateTime.Now
            };
            _dbContext.InventoryLogs.Add(log);
            logs.Add(log);

            // 扣减批次
            batch.Quantity -= deductQty;
            if (batch.Quantity == 0)
                batch.Status = 2;
            batch.UpdatedTime = DateTime.Now;

            currentInventoryQuantity -= deductQty;
            remaining -= deductQty;
        }

        // 一次 SaveChanges，EF Core 自动填充 log.Id
        await _dbContext.SaveChangesAsync();
        var logIds = logs.Select(l => l.Id).ToList();

        return DeductResult.Ok(logIds, quantityToDeduct, currentInventoryQuantity);
    }

    /// <summary>
    /// 手动指定模式扣减：按用户指定的批次明细逐项扣减
    /// </summary>
    private async Task<DeductResult> DeductByManualAsync(
        OutboundCreateDto dto, long tenantId, long storeId,
        decimal currentInventoryQuantity, string operatorName)
    {
        var logs = new List<InventoryLog>();
        var totalDeducted = 0m;

        foreach (var item in dto.BatchItems!)
        {
            // 校验批次
            var batch = await _dbContext.InventoryBatches
                .FirstOrDefaultAsync(b => b.Id == item.BatchId
                    && b.ProductId == dto.ProductId
                    && b.TenantId == tenantId
                    && b.StoreId == storeId
                    && b.Status == 1);
            if (batch == null)
                return DeductResult.Fail($"批次不存在或已用完（BatchId={item.BatchId}）");
            if (batch.Quantity < item.Quantity)
                return DeductResult.Fail($"批次 {batch.BatchNo} 库存不足");

            // 写 InventoryLog（先收集，循环结束后统一 SaveChanges）
            var log = new InventoryLog
            {
                ProductId = dto.ProductId,
                Type = 2,
                SourceType = dto.SourceType,
                UnitPrice = dto.UnitPrice ?? batch.UnitPrice,
                Quantity = -item.Quantity,
                BeforeQuantity = currentInventoryQuantity,
                AfterQuantity = currentInventoryQuantity - item.Quantity,
                BatchNo = batch.BatchNo,
                ExpirationDate = batch.ExpirationDate,
                Remark = dto.Remark,
                OperatorName = operatorName,
                TenantId = tenantId,
                TenantCode = _currentUser.TenantCode ?? string.Empty,
                StoreId = storeId,
                StoreCode = _currentUser.StoreCode ?? string.Empty,
                CreatedTime = DateTime.Now
            };
            _dbContext.InventoryLogs.Add(log);
            logs.Add(log);

            // 扣减批次
            batch.Quantity -= item.Quantity;
            if (batch.Quantity == 0)
                batch.Status = 2;
            batch.UpdatedTime = DateTime.Now;

            currentInventoryQuantity -= item.Quantity;
            totalDeducted += item.Quantity;
        }

        // 一次 SaveChanges，EF Core 自动填充 log.Id
        await _dbContext.SaveChangesAsync();
        var logIds = logs.Select(l => l.Id).ToList();

        return DeductResult.Ok(logIds, totalDeducted, currentInventoryQuantity);
    }

    /// <summary>
    /// 扣减结果内部载体
    /// </summary>
    private record DeductResult(bool Success, List<long>? LogIds, decimal TotalQuantity, decimal AfterQuantity, string? ErrorMessage)
    {
        public static DeductResult Ok(List<long> logIds, decimal totalQuantity, decimal afterQuantity) =>
            new(true, logIds, totalQuantity, afterQuantity, null);

        public static DeductResult Fail(string errorMessage) =>
            new(false, null, 0, 0, errorMessage);
    }
}
