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
/// 入库应用服务实现
/// 事务内写 InventoryLog + InventoryBatch + Inventory 三表，自动维护库存与批次
/// </summary>
public class InboundAppService : IInboundAppService
{
    private readonly StoreDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<InboundCreateDto> _validator;
    private readonly ILogger<InboundAppService> _logger;

    public InboundAppService(
        StoreDbContext dbContext,
        ICurrentUser currentUser,
        IValidator<InboundCreateDto> validator,
        ILogger<InboundAppService> logger)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _validator = validator;
        _logger = logger;
    }

    /// <summary>
    /// 创建入库记录
    /// 事务边界：全程在 IDbContextTransaction 内执行，三表写入要么全部成功要么全部回滚
    /// </summary>
    public async Task<ApiResponseDto<InventoryLogDto>> CreateAsync(InboundCreateDto dto)
    {
        if (!_currentUser.TenantId.HasValue || !_currentUser.StoreId.HasValue)
            return ApiResponseDto<InventoryLogDto>.Fail("无法确定当前租户或门店", 401);

        var validation = await _validator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<InventoryLogDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId.Value;

        // log 在 try 块外声明，便于提交事务后重新查询填充显示字段
        InventoryLog log = null!;
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
                return ApiResponseDto<InventoryLogDto>.Fail("商品不存在", 400);

            // 2. 采购入库校验供应商存在
            if (dto.SourceType == InventoryLogSourceTypes.PurchaseInbound)
            {
                if (!dto.SupplierId.HasValue)
                    return ApiResponseDto<InventoryLogDto>.Fail("采购入库必须选择供应商", 400);

                var supplierExists = await _dbContext.Suppliers
                    .AnyAsync(s => s.Id == dto.SupplierId.Value
                        && s.TenantId == tenantId
                        && s.StoreId == storeId
                        && !s.IsDeleted);
                if (!supplierExists)
                    return ApiResponseDto<InventoryLogDto>.Fail("供应商不存在", 400);
            }

            // 3. 查当前库存汇总记录，计算 BeforeQuantity / AfterQuantity
            var inventory = await _dbContext.Inventories
                .FirstOrDefaultAsync(i => i.ProductId == dto.ProductId
                    && i.TenantId == tenantId
                    && i.StoreId == storeId);
            var beforeQuantity = inventory?.Quantity ?? 0m;
            var afterQuantity = beforeQuantity + dto.Quantity;

            // 4. 写 InventoryLog（Type=1 入库）
            log = new InventoryLog
            {
                ProductId = dto.ProductId,
                Type = 1,
                SourceType = dto.SourceType,
                SupplierId = dto.SupplierId,
                UnitPrice = dto.UnitPrice,
                Quantity = dto.Quantity,
                BeforeQuantity = beforeQuantity,
                AfterQuantity = afterQuantity,
                BatchNo = dto.BatchNo,
                ExpirationDate = dto.ExpirationDate,
                Remark = dto.Remark,
                OperatorName = _currentUser.RealName ?? _currentUser.UserName,
                TenantId = tenantId,
                TenantCode = _currentUser.TenantCode ?? string.Empty,
                StoreId = storeId,
                StoreCode = _currentUser.StoreCode ?? string.Empty,
                CreatedTime = DateTime.Now
            };
            _dbContext.InventoryLogs.Add(log);

            // 5. 写 InventoryBatch（始终新建，不累加、不覆盖，避免破坏 FEFO 排序）
            var batch = new InventoryBatch
            {
                ProductId = dto.ProductId,
                BatchNo = dto.BatchNo ?? string.Empty,
                Quantity = dto.Quantity,
                UnitPrice = dto.UnitPrice ?? 0m,
                ProductionDate = dto.ProductionDate,
                ShelfLifeDays = dto.ShelfLifeDays,
                ExpirationDate = dto.ExpirationDate,
                PurchaseDate = DateTime.Today,
                Status = 1,
                TenantId = tenantId,
                TenantCode = _currentUser.TenantCode ?? string.Empty,
                StoreId = storeId,
                StoreCode = _currentUser.StoreCode ?? string.Empty,
                CreatedTime = DateTime.Now
            };
            _dbContext.InventoryBatches.Add(batch);

            // 6. 更新 Inventory 汇总表（不存在则插入，存在则累加）
            if (inventory == null)
            {
                _dbContext.Inventories.Add(new Inventory
                {
                    ProductId = dto.ProductId,
                    Quantity = dto.Quantity,
                    AlertQuantity = 0m,
                    TenantId = tenantId,
                    TenantCode = _currentUser.TenantCode ?? string.Empty,
                    StoreId = storeId,
                    StoreCode = _currentUser.StoreCode ?? string.Empty,
                    CreatedTime = DateTime.Now
                });
            }
            else
            {
                inventory.Quantity = afterQuantity;
                inventory.UpdatedTime = DateTime.Now;
            }

            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (DbUpdateException ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "入库失败：ProductId={ProductId}, SourceType={SourceType}", dto.ProductId, dto.SourceType);
            return ApiResponseDto<InventoryLogDto>.Fail("入库失败，请重试", 500);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "入库异常：ProductId={ProductId}, SourceType={SourceType}", dto.ProductId, dto.SourceType);
            return ApiResponseDto<InventoryLogDto>.Fail("入库失败，请重试", 500);
        }

        // 事务已提交，重新查询填充显示字段（移出 try 块：若此查询失败，数据已落库，
        // 不应回滚已提交事务或返回 500 让用户误重试导致重复入库）
        var logWithNav = await _dbContext.InventoryLogs
            .Include(l => l.Product)
            .Include(l => l.Supplier)
            .FirstAsync(l => l.Id == log.Id);

        var result = new InventoryLogDto
        {
            Id = logWithNav.Id,
            ProductId = logWithNav.ProductId,
            Type = logWithNav.Type,
            SourceType = logWithNav.SourceType,
            SupplierId = logWithNav.SupplierId,
            UnitPrice = logWithNav.UnitPrice,
            Quantity = logWithNav.Quantity,
            BeforeQuantity = logWithNav.BeforeQuantity,
            AfterQuantity = logWithNav.AfterQuantity,
            BatchNo = logWithNav.BatchNo,
            ExpirationDate = logWithNav.ExpirationDate,
            RelatedId = logWithNav.RelatedId,
            Remark = logWithNav.Remark,
            CreatedAt = logWithNav.CreatedTime,
            UpdatedAt = logWithNav.UpdatedTime,
            ProductName = logWithNav.Product?.Name,
            ProductCode = logWithNav.Product?.Code,
            SupplierName = logWithNav.Supplier?.Name,
            OperatorName = logWithNav.OperatorName
        };
        return ApiResponseDto<InventoryLogDto>.Ok(result, "入库成功");
    }
}
