using Mapster;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Bms.BuildingBlocks.Abstractions.Security;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.PurchaseReturns;
using Bms.Store.Domain.Entities;
using PurchaseReturnEntity = Bms.Store.Domain.Entities.PurchaseReturn;
using Bms.Store.Infrastructure;

namespace Bms.Store.Application.Services;

/// <summary>
/// 采购退货应用服务实现
/// 支持一次退回多种商品：主表存储汇总信息，明细表存储各商品退货数量与退款金额
/// 创建退货时联动库存扣减（按批次或 FIFO）并冲减关联采购订单的已退货金额
/// </summary>
public class PurchaseReturnAppService : IPurchaseReturnAppService
{
    private readonly StoreDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IInventoryAppService _inventoryAppService;
    private readonly IValidator<PurchaseReturnCreateDto> _createValidator;
    private readonly IValidator<PurchaseReturnUpdateDto> _updateValidator;
    private readonly IInventoryAlertAppService _alertAppService;

    public PurchaseReturnAppService(
        StoreDbContext dbContext,
        ICurrentUser currentUser,
        IInventoryAppService inventoryAppService,
        IValidator<PurchaseReturnCreateDto> createValidator,
        IValidator<PurchaseReturnUpdateDto> updateValidator,
        IInventoryAlertAppService alertAppService)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _inventoryAppService = inventoryAppService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _alertAppService = alertAppService;
    }

    /// <summary>
    /// 获取采购退货分页列表（包含明细）
    /// </summary>
    public async Task<ApiResponseDto<PagedResponseDto<PurchaseReturnDto>>> GetPagedListAsync(PurchaseReturnQueryDto query)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<PagedResponseDto<PurchaseReturnDto>>.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId ?? 0;
        var queryable = _dbContext.PurchaseReturns
            .Include(p => p.Items)
            .Where(p => p.TenantId == tenantId && p.StoreId == storeId);

        if (!string.IsNullOrWhiteSpace(query.ReturnNo))
            queryable = queryable.Where(p => p.ReturnNo.Contains(query.ReturnNo));
        if (query.SupplierId.HasValue)
            queryable = queryable.Where(p => p.SupplierId == query.SupplierId.Value);

        var total = await queryable.CountAsync();
        var items = await queryable
            .OrderByDescending(p => p.CreatedTime)
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var result = new PagedResponseDto<PurchaseReturnDto>
        {
            List = items.Adapt<List<PurchaseReturnDto>>(),
            Total = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
        return ApiResponseDto<PagedResponseDto<PurchaseReturnDto>>.Ok(result);
    }

    /// <summary>
    /// 根据ID获取采购退货详情（包含明细）
    /// </summary>
    public async Task<ApiResponseDto<PurchaseReturnDto?>> GetByIdAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<PurchaseReturnDto?>.Fail("登录状态异常，请重新登录", 401);

        var entity = await _dbContext.PurchaseReturns
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == _currentUser.TenantId.Value && p.StoreId == (_currentUser.StoreId ?? 0));
        if (entity == null)
            return ApiResponseDto<PurchaseReturnDto?>.Fail("采购退货不存在", 404);
        return ApiResponseDto<PurchaseReturnDto?>.Ok(entity.Adapt<PurchaseReturnDto>());
    }

    /// <summary>
    /// 创建采购退货：级联创建主表与明细表，自动汇总总数量与总退款金额
    /// 联动库存出库：按明细 BatchNo 扣减指定批次，未指定时按 FIFO 扣减
    /// 联动成本冲减：若关联原采购订单，累加 PurchaseOrder.RefundedAmount
    /// 任一明细库存不足则整体回滚
    /// </summary>
    public async Task<ApiResponseDto<PurchaseReturnDto>> CreateAsync(PurchaseReturnCreateDto dto)
    {
        if (!_currentUser.TenantId.HasValue || !_currentUser.StoreId.HasValue)
            return ApiResponseDto<PurchaseReturnDto>.Fail("登录状态异常，请重新登录", 401);

        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<PurchaseReturnDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var tenantCode = _currentUser.TenantCode ?? string.Empty;
        var storeId = _currentUser.StoreId.Value;

        // 关联采购订单校验：若传入 PurchaseOrderId，必须存在且供应商匹配
        if (dto.PurchaseOrderId.HasValue)
        {
            var purchaseOrder = await _dbContext.PurchaseOrders
                .Include(p => p.OrderItems)
                .FirstOrDefaultAsync(p => p.Id == dto.PurchaseOrderId.Value && p.TenantId == tenantId && p.StoreId == storeId);
            if (purchaseOrder == null)
                return ApiResponseDto<PurchaseReturnDto>.Fail("关联的采购订单不存在", 404);
            if (!purchaseOrder.OrderItems.Any(i => i.SupplierId == dto.SupplierId))
                return ApiResponseDto<PurchaseReturnDto>.Fail("退货供应商与采购订单明细供应商不一致", 400);
        }

        var now = DateTime.Now;

        // 映射主表与明细（Mapster 自动映射 Items 列表）
        var entity = dto.Adapt<PurchaseReturnEntity>();
        entity.TenantId = tenantId;
        entity.TenantCode = tenantCode;
        entity.StoreId = storeId;
        entity.CreatedTime = now;
        entity.TotalQuantity = dto.Items.Sum(i => i.Quantity);
        entity.TotalRefundAmount = dto.Items.Sum(i => i.RefundAmount);
        // 操作人从当前登录用户取值，不依赖前端传入，避免遗漏记录
        entity.OperatorId = _currentUser.UserId;

        // 设置明细审计字段
        foreach (var item in entity.Items)
        {
            item.TenantId = tenantId;
            item.TenantCode = tenantCode;
            item.StoreId = storeId;
            item.CreatedTime = now;
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            // 事务级顾问锁：按 (租户, 门店, 退货日期) 串行化并发请求
            // ReturnNo 采用"查max+1"生成模式，并发下需串行化避免重复
            var lockKey = PurchaseReturnNoGenerator.BuildLockKey(tenantId, storeId, dto.ReturnTime);
            await _dbContext.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock({0})", lockKey);

            // 在锁保护下生成退货单号（PR{yyyyMMdd}{序号}）
            entity.ReturnNo = await PurchaseReturnNoGenerator.GenerateAsync(_dbContext, tenantId, storeId, dto.ReturnTime);

            _dbContext.PurchaseReturns.Add(entity);
            await _dbContext.SaveChangesAsync();

            // 扣减库存：按明细逐条调用，未指定 BatchNo 时由 DeductByBatchAsync 按 FIFO 扣减
            foreach (var item in entity.Items)
            {
                var deductResult = await _inventoryAppService.DeductByBatchAsync(
                    item.ProductId,
                    item.Quantity,
                    item.BatchNo,
                    sourceType: InventoryLogSourceTypes.PurchaseReturnOutbound,
                    refId: entity.Id,
                    remark: entity.ReturnNo);

                if (!deductResult.IsSuccess)
                    throw new InvalidOperationException(deductResult.Message ?? "库存扣减失败");

                // 同事务内逐条 SaveChanges，确保后续明细 FIFO 查询能看到已扣减的批次库存
                await _dbContext.SaveChangesAsync();
            }

            // 冲减关联采购订单的已退货金额
            if (dto.PurchaseOrderId.HasValue)
            {
                var purchaseOrder = await _dbContext.PurchaseOrders
                    .FirstOrDefaultAsync(p => p.Id == dto.PurchaseOrderId.Value && p.TenantId == tenantId && p.StoreId == storeId);
                if (purchaseOrder != null)
                {
                    purchaseOrder.RefundedAmount += entity.TotalRefundAmount;
                    purchaseOrder.UpdatedTime = now;
                }
            }

            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            // 即时检测预警（失败不影响退货结果，定时任务兜底）
            foreach (var item in entity.Items)
            {
                try
                {
                    await _alertAppService.CheckInventoryAlertsAsync(tenantId, storeId, item.ProductId);
                }
                catch
                {
                    // 预警检测失败不影响主流程
                }
            }

            return ApiResponseDto<PurchaseReturnDto>.Ok(entity.Adapt<PurchaseReturnDto>(), "创建成功");
        }
        catch (InvalidOperationException ex)
        {
            await transaction.RollbackAsync();
            return ApiResponseDto<PurchaseReturnDto>.Fail(ex.Message, 400);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// 更新采购退货：替换明细并重新汇总总数量与总退款金额
    /// 业务约束：库存扣减与成本冲减仅在 CreateAsync 触发，本方法不调整库存与 RefundedAmount。
    /// 若需调整已创建退货单的明细数量，应作废原单并重新创建，避免库存数据不一致。
    /// </summary>
    public async Task<ApiResponseDto<PurchaseReturnDto>> UpdateAsync(PurchaseReturnUpdateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<PurchaseReturnDto>.Fail("登录状态异常，请重新登录", 401);

        var validation = await _updateValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<PurchaseReturnDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId ?? 0;
        var entity = await _dbContext.PurchaseReturns
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == dto.Id && p.TenantId == tenantId && p.StoreId == storeId);
        if (entity == null)
            return ApiResponseDto<PurchaseReturnDto>.Fail("采购退货不存在", 404);

        // 单号变更时检查唯一性
        if (entity.ReturnNo != dto.ReturnNo)
        {
            var returnNoExists = await _dbContext.PurchaseReturns
                .AnyAsync(p => p.ReturnNo == dto.ReturnNo && p.TenantId == tenantId && p.StoreId == storeId && p.Id != dto.Id);
            if (returnNoExists)
                return ApiResponseDto<PurchaseReturnDto>.Fail($"退货单号 {dto.ReturnNo} 已存在", 400);
        }

        var now = DateTime.Now;

        // 替换明细：先删除旧明细，再添加新明细
        if (entity.Items.Any())
            _dbContext.PurchaseReturnItems.RemoveRange(entity.Items);

        var tenantCode = _currentUser.TenantCode ?? string.Empty;
        foreach (var itemDto in dto.Items)
        {
            entity.Items.Add(new PurchaseReturnItem
            {
                ProductId = itemDto.ProductId,
                Quantity = itemDto.Quantity,
                RefundAmount = itemDto.RefundAmount,
                BatchNo = itemDto.BatchNo,
                Remark = itemDto.Remark,
                TenantId = tenantId,
                TenantCode = tenantCode,
                StoreId = storeId,
                CreatedTime = now
            });
        }

        // 更新主表字段
        entity.ReturnNo = dto.ReturnNo;
        entity.SupplierId = dto.SupplierId;
        entity.PurchaseOrderId = dto.PurchaseOrderId;
        entity.ReturnTime = dto.ReturnTime;
        entity.VoucherImageUrl = dto.VoucherImageUrl;
        // 操作人记录的是"创建退货单的人"，更新时不可被改写
        entity.Remark = dto.Remark;
        entity.TotalQuantity = dto.Items.Sum(i => i.Quantity);
        entity.TotalRefundAmount = dto.Items.Sum(i => i.RefundAmount);
        entity.UpdatedTime = now;

        await _dbContext.SaveChangesAsync();
        return ApiResponseDto<PurchaseReturnDto>.Ok(entity.Adapt<PurchaseReturnDto>(), "更新成功");
    }

    /// <summary>
    /// 删除采购退货（级联删除明细由数据库外键约束保证）
    /// 业务约束：库存扣减与成本冲减不可逆，本方法不回滚已扣减库存与 RefundedAmount。
    /// 生产环境建议改为"作废"状态保留审计轨迹，避免直接物理删除导致库存数据不一致。
    /// </summary>
    public async Task<ApiResponseDto> DeleteAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("登录状态异常，请重新登录", 401);

        var entity = await _dbContext.PurchaseReturns
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == _currentUser.TenantId.Value && p.StoreId == (_currentUser.StoreId ?? 0));
        if (entity == null)
            return ApiResponseDto.Fail("采购退货不存在", 404);

        _dbContext.PurchaseReturns.Remove(entity);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, "删除成功");
    }

    /// <summary>
    /// 批量删除采购退货
    /// </summary>
    public async Task<ApiResponseDto> BatchDeleteAsync(List<long> ids)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("登录状态异常，请重新登录", 401);
        if (ids == null || !ids.Any())
            return ApiResponseDto.Fail("请选择要删除的数据", 400);

        var entities = await _dbContext.PurchaseReturns
            .Where(p => ids.Contains(p.Id) && p.TenantId == _currentUser.TenantId.Value && p.StoreId == (_currentUser.StoreId ?? 0))
            .ToListAsync();

        _dbContext.PurchaseReturns.RemoveRange(entities);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, $"成功删除 {entities.Count} 条数据");
    }
}
