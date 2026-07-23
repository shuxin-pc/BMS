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
    /// </summary>
    public async Task<ApiResponseDto<PagedResponseDto<InventoryDto>>> GetPagedListAsync(InventoryQueryDto query)
    {
        if (!_currentUser.TenantId.HasValue || !_currentUser.StoreId.HasValue)
            return ApiResponseDto<PagedResponseDto<InventoryDto>>.Fail("无法确定当前租户或门店", 401);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId.Value;
        // 正品库存查询：排除样品(4)/赠品(5)，避免与 SampleGiftAppService 管理范围重叠
        var queryable = from i in _dbContext.Inventories
                        join p in _dbContext.Products on i.ProductId equals p.Id
                        where i.TenantId == tenantId && i.StoreId == storeId
                            && p.Type != 4 && p.Type != 5
                            && !p.IsDeleted
                        select i;

        if (query.ProductId.HasValue)
            queryable = queryable.Where(i => i.ProductId == query.ProductId.Value);

        var total = await queryable.CountAsync();
        var items = await queryable
            .OrderByDescending(i => i.CreatedTime)
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var result = new PagedResponseDto<InventoryDto>
        {
            List = items.Adapt<List<InventoryDto>>(),
            Total = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
        return ApiResponseDto<PagedResponseDto<InventoryDto>>.Ok(result);
    }

    /// <summary>
    /// 根据ID获取库存详情
    /// </summary>
    public async Task<ApiResponseDto<InventoryDto?>> GetByIdAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue || !_currentUser.StoreId.HasValue)
            return ApiResponseDto<InventoryDto?>.Fail("无法确定当前租户或门店", 401);

        var entity = await _dbContext.Inventories
            .FirstOrDefaultAsync(i => i.Id == id && i.TenantId == _currentUser.TenantId.Value && i.StoreId == _currentUser.StoreId.Value);
        if (entity == null)
            return ApiResponseDto<InventoryDto?>.Fail("库存不存在", 404);
        return ApiResponseDto<InventoryDto?>.Ok(entity.Adapt<InventoryDto>());
    }

    /// <summary>
    /// 创建库存
    /// </summary>
    public async Task<ApiResponseDto<InventoryDto>> CreateAsync(InventoryCreateDto dto)
    {
        if (!_currentUser.TenantId.HasValue || !_currentUser.StoreId.HasValue)
            return ApiResponseDto<InventoryDto>.Fail("无法确定当前租户或门店", 401);

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
            return ApiResponseDto<InventoryDto>.Fail("无法确定当前租户或门店", 401);

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
        entity.AlertQuantity = dto.AlertQuantity;
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
            return ApiResponseDto.Fail("无法确定当前租户或门店", 401);

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
            return ApiResponseDto.Fail("无法确定当前租户或门店", 401);
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
            return ApiResponseDto.Fail("无法确定当前租户或门店", 401);
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
                AlertQuantity = 0,
                TenantId = tenantId,
                TenantCode = tenantCode,
                StoreId = storeId,
                CreatedTime = now
            };
            _dbContext.Inventories.Add(inventory);
        }

        return ApiResponseDto.Success(null, "扣减成功");
    }
}
