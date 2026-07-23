using Mapster;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Bms.BuildingBlocks.Abstractions.Security;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.StockTransfers;
using Bms.Store.Domain.Entities;
using StockTransferEntity = Bms.Store.Domain.Entities.StockTransfer;
using Bms.Store.Infrastructure;

namespace Bms.Store.Application.Services;

/// <summary>
/// 库存调拨单应用服务实现
/// </summary>
public class StockTransferAppService : IStockTransferAppService
{
    private readonly StoreDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<StockTransferCreateDto> _createValidator;
    private readonly IValidator<StockTransferUpdateDto> _updateValidator;
    private readonly IInventoryAlertAppService _alertAppService;

    public StockTransferAppService(
        StoreDbContext dbContext,
        ICurrentUser currentUser,
        IValidator<StockTransferCreateDto> createValidator,
        IValidator<StockTransferUpdateDto> updateValidator,
        IInventoryAlertAppService alertAppService)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _alertAppService = alertAppService;
    }

    /// <summary>
    /// 获取库存调拨单分页列表
    /// </summary>
    public async Task<ApiResponseDto<PagedResponseDto<StockTransferDto>>> GetPagedListAsync(StockTransferQueryDto query)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<PagedResponseDto<StockTransferDto>>.Fail("无法确定当前租户", 401);

        var tenantId = _currentUser.TenantId.Value;
        var queryable = _dbContext.StockTransfers
            .Where(t => t.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(query.TransferNo))
            queryable = queryable.Where(t => t.TransferNo.Contains(query.TransferNo));
        if (query.FromStoreId.HasValue)
            queryable = queryable.Where(t => t.FromStoreId == query.FromStoreId.Value);
        if (query.ToStoreId.HasValue)
            queryable = queryable.Where(t => t.ToStoreId == query.ToStoreId.Value);
        if (query.Status.HasValue)
            queryable = queryable.Where(t => t.Status == query.Status.Value);

        var total = await queryable.CountAsync();
        var items = await queryable
            .OrderByDescending(t => t.CreatedTime)
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var result = new PagedResponseDto<StockTransferDto>
        {
            List = items.Adapt<List<StockTransferDto>>(),
            Total = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
        return ApiResponseDto<PagedResponseDto<StockTransferDto>>.Ok(result);
    }

    /// <summary>
    /// 根据ID获取库存调拨单详情
    /// </summary>
    public async Task<ApiResponseDto<StockTransferDto?>> GetByIdAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<StockTransferDto?>.Fail("无法确定当前租户", 401);

        var entity = await _dbContext.StockTransfers
            .FirstOrDefaultAsync(t => t.Id == id && t.TenantId == _currentUser.TenantId.Value);
        if (entity == null)
            return ApiResponseDto<StockTransferDto?>.Fail("库存调拨单不存在", 404);
        return ApiResponseDto<StockTransferDto?>.Ok(entity.Adapt<StockTransferDto>());
    }

    /// <summary>
    /// 创建库存调拨单（草稿状态：待调出，不调整库存）
    /// </summary>
    public async Task<ApiResponseDto<StockTransferDto>> CreateAsync(StockTransferCreateDto dto)
    {
        if (!_currentUser.TenantId.HasValue || !_currentUser.StoreId.HasValue)
            return ApiResponseDto<StockTransferDto>.Fail("无法确定当前租户或门店", 401);

        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<StockTransferDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId.Value;

        // 检查调拨单号唯一性
        var noExists = await _dbContext.StockTransfers
            .AnyAsync(t => t.TransferNo == dto.TransferNo && t.TenantId == tenantId );
        if (noExists)
            return ApiResponseDto<StockTransferDto>.Fail($"调拨单号 {dto.TransferNo} 已存在", 400);

        var entity = dto.Adapt<StockTransferEntity>();
        entity.TenantId = tenantId;
        entity.TenantCode = _currentUser.TenantCode ?? string.Empty;
        entity.StoreId = storeId;
        entity.Status = StockTransferStatus.Draft; // 待调出（草稿）
        entity.CreatedTime = DateTime.Now;

        _dbContext.StockTransfers.Add(entity);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto<StockTransferDto>.Ok(entity.Adapt<StockTransferDto>(), "创建成功");
    }

    /// <summary>
    /// 更新库存调拨单
    /// </summary>
    public async Task<ApiResponseDto<StockTransferDto>> UpdateAsync(StockTransferUpdateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<StockTransferDto>.Fail("无法确定当前租户", 401);

        var validation = await _updateValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<StockTransferDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var entity = await _dbContext.StockTransfers
            .FirstOrDefaultAsync(t => t.Id == dto.Id && t.TenantId == tenantId);
        if (entity == null)
            return ApiResponseDto<StockTransferDto>.Fail("库存调拨单不存在", 404);

        // 仅待调出（草稿）状态可修改；已调入/已取消为终态
        if (entity.Status != StockTransferStatus.Draft)
            return ApiResponseDto<StockTransferDto>.Fail($"仅待调出状态的调拨单可修改（当前状态：{entity.Status}）", 400);

        // 调拨单号变更时检查唯一性
        if (entity.TransferNo != dto.TransferNo)
        {
            var noExists = await _dbContext.StockTransfers
                .AnyAsync(t => t.TransferNo == dto.TransferNo && t.TenantId == tenantId && t.Id != dto.Id);
            if (noExists)
                return ApiResponseDto<StockTransferDto>.Fail($"调拨单号 {dto.TransferNo} 已存在", 400);
        }

        entity.TransferNo = dto.TransferNo;
        entity.FromStoreId = dto.FromStoreId;
        entity.FromStoreCode = dto.FromStoreCode;
        entity.ToStoreId = dto.ToStoreId;
        entity.ToStoreCode = dto.ToStoreCode;
        entity.TransferDate = dto.TransferDate;
        // Status 不允许通过 Update 直接修改，只能通过 ExecuteAsync/CancelAsync 流转
        entity.OperatorId = dto.OperatorId;
        entity.Remark = dto.Remark;
        entity.UpdatedTime = DateTime.Now;

        await _dbContext.SaveChangesAsync();
        return ApiResponseDto<StockTransferDto>.Ok(entity.Adapt<StockTransferDto>(), "更新成功");
    }

    /// <summary>
    /// 删除库存调拨单（仅待调出/已取消可删除；已调入为终态不可删除）
    /// </summary>
    public async Task<ApiResponseDto> DeleteAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("无法确定当前租户", 401);

        var entity = await _dbContext.StockTransfers
            .FirstOrDefaultAsync(t => t.Id == id && t.TenantId == _currentUser.TenantId.Value);
        if (entity == null)
            return ApiResponseDto.Fail("库存调拨单不存在", 404);

        if (entity.Status == StockTransferStatus.Completed)
            return ApiResponseDto.Fail("已调入的调拨单不可删除，如需调整请创建反向调拨单", 400);

        _dbContext.StockTransfers.Remove(entity);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, "删除成功");
    }

    /// <summary>
    /// 批量删除库存调拨单（仅待调出/已取消可删除；已调入会被跳过）
    /// </summary>
    public async Task<ApiResponseDto> BatchDeleteAsync(List<long> ids)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("无法确定当前租户", 401);
        if (ids == null || !ids.Any())
            return ApiResponseDto.Fail("请选择要删除的数据", 400);

        var entities = await _dbContext.StockTransfers
            .Where(t => ids.Contains(t.Id) && t.TenantId == _currentUser.TenantId.Value)
            .ToListAsync();

        var deletable = entities.Where(t => t.Status != StockTransferStatus.Completed).ToList();
        var skipped = entities.Count - deletable.Count;

        _dbContext.StockTransfers.RemoveRange(deletable);
        await _dbContext.SaveChangesAsync();

        var message = $"成功删除 {deletable.Count} 条数据";
        if (skipped > 0)
            message += $"，跳过 {skipped} 条已调入调拨单（不可删除）";

        return ApiResponseDto.Success(null, message);
    }

    /// <summary>
    /// 执行调拨：同一事务内调出门店库存扣减（SourceType=调拨出库）+ 调入门店库存增加（SourceType=调拨入库），
    /// 写两条 InventoryLog 并关联 RefId=StockTransfer.Id，状态转已调入（已完成）
    /// 仅待调出（草稿）状态可执行
    /// </summary>
    public async Task<ApiResponseDto> ExecuteAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("无法确定当前租户", 401);

        var tenantId = _currentUser.TenantId.Value;
        var tenantCode = _currentUser.TenantCode ?? string.Empty;
        var now = DateTime.Now;

        var transfer = await _dbContext.StockTransfers
            .FirstOrDefaultAsync(t => t.Id == id && t.TenantId == tenantId);
        if (transfer == null)
            return ApiResponseDto.Fail("库存调拨单不存在", 404);

        // 仅待调出（草稿）状态可执行；已调入/已取消为终态
        if (transfer.Status != StockTransferStatus.Draft)
            return ApiResponseDto.Fail($"仅待调出状态的调拨单可执行（当前状态：{transfer.Status}）", 400);

        var items = await _dbContext.StockTransferItems
            .Where(si => si.StockTransferId == id && si.TenantId == tenantId)
            .ToListAsync();

        if (!items.Any())
            return ApiResponseDto.Fail("调拨明细为空，无法执行", 400);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            foreach (var item in items)
            {
                // 调出方：扣减库存
                var fromInventory = await _dbContext.Inventories
                    .FirstOrDefaultAsync(inv => inv.ProductId == item.ProductId
                        && inv.TenantId == tenantId
                        && inv.StoreId == transfer.FromStoreId);

                var fromBeforeQty = fromInventory?.Quantity ?? 0;
                if (fromInventory == null)
                    return ApiResponseDto.Fail($"调出门店商品(ID:{item.ProductId})无库存记录", 400);

                if (fromInventory.Quantity < item.Quantity)
                    return ApiResponseDto.Fail($"调出门店商品(ID:{item.ProductId})库存不足（当前 {fromInventory.Quantity}，需调拨 {item.Quantity}）", 400);

                fromInventory.Quantity -= item.Quantity;
                fromInventory.UpdatedTime = now;

                _dbContext.InventoryLogs.Add(new InventoryLog
                {
                    ProductId = item.ProductId,
                    Type = 2, // 出库
                    SourceType = InventoryLogSourceTypes.TransferOutbound, // 调拨出库
                    Quantity = -item.Quantity,
                    BeforeQuantity = fromBeforeQty,
                    AfterQuantity = fromInventory.Quantity,
                    BatchNo = item.BatchNo,
                    RelatedId = transfer.Id,
                    Remark = $"调拨出库-{transfer.TransferNo}",
                    TenantId = tenantId,
                    TenantCode = tenantCode,
                    StoreId = transfer.FromStoreId,
                    CreatedTime = now
                });

                // 调入方：增加库存
                var toInventory = await _dbContext.Inventories
                    .FirstOrDefaultAsync(inv => inv.ProductId == item.ProductId
                        && inv.TenantId == tenantId
                        && inv.StoreId == transfer.ToStoreId);

                var toBeforeQty = toInventory?.Quantity ?? 0;
                if (toInventory == null)
                {
                    toInventory = new Inventory
                    {
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        AlertQuantity = 0,
                        TenantId = tenantId,
                        TenantCode = tenantCode,
                        StoreId = transfer.ToStoreId,
                        CreatedTime = now
                    };
                    _dbContext.Inventories.Add(toInventory);
                }
                else
                {
                    toInventory.Quantity += item.Quantity;
                    toInventory.UpdatedTime = now;
                }

                _dbContext.InventoryLogs.Add(new InventoryLog
                {
                    ProductId = item.ProductId,
                    Type = 1, // 入库
                    SourceType = InventoryLogSourceTypes.TransferInbound, // 调拨入库
                    Quantity = item.Quantity,
                    BeforeQuantity = toBeforeQty,
                    AfterQuantity = toBeforeQty + item.Quantity,
                    BatchNo = item.BatchNo,
                    RelatedId = transfer.Id,
                    Remark = $"调拨入库-{transfer.TransferNo}",
                    TenantId = tenantId,
                    TenantCode = tenantCode,
                    StoreId = transfer.ToStoreId,
                    CreatedTime = now
                });
            }

            // 更新调拨单状态为已调入（已完成）
            transfer.Status = StockTransferStatus.Completed;
            transfer.UpdatedTime = now;

            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            // 调出方库存减少，即时检测低库存预警
            foreach (var item in items)
            {
                try
                {
                    await _alertAppService.CheckLowStockAsync(tenantId, transfer.FromStoreId, item.ProductId);
                }
                catch
                {
                    // 预警检测失败不影响主流程，定时任务会兜底
                }
            }

            return ApiResponseDto.Success(null, "调拨执行成功");
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// 取消调拨单：草稿转已取消（已调入的不可取消，需走反向调拨单）
    /// </summary>
    public async Task<ApiResponseDto> CancelAsync(long id, string? reason)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("无法确定当前租户", 401);

        var tenantId = _currentUser.TenantId.Value;
        var entity = await _dbContext.StockTransfers
            .FirstOrDefaultAsync(t => t.Id == id && t.TenantId == tenantId);
        if (entity == null)
            return ApiResponseDto.Fail("库存调拨单不存在", 404);

        // 仅待调出（草稿）状态可取消；已调入为终态，需走反向调拨单
        if (entity.Status != StockTransferStatus.Draft)
            return ApiResponseDto.Fail($"仅待调出状态的调拨单可取消（当前状态：{entity.Status}），已调入调拨不可取消，请创建反向调拨单", 400);

        entity.Status = StockTransferStatus.Cancelled;
        if (!string.IsNullOrWhiteSpace(reason))
            entity.Remark = string.IsNullOrWhiteSpace(entity.Remark) ? $"取消原因：{reason}" : $"{entity.Remark}；取消原因：{reason}";
        entity.UpdatedTime = DateTime.Now;

        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, "取消成功");
    }
}
