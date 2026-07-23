using Mapster;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Bms.BuildingBlocks.Abstractions.Security;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.PurchaseOrders;
using Bms.Store.Domain.Entities;
using PurchaseOrderEntity = Bms.Store.Domain.Entities.PurchaseOrder;
using Bms.Store.Infrastructure;

namespace Bms.Store.Application.Services;

/// <summary>
/// 采购订单应用服务实现
/// </summary>
public class PurchaseOrderAppService : IPurchaseOrderAppService
{
    private readonly StoreDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<PurchaseOrderCreateDto> _createValidator;
    private readonly IValidator<PurchaseOrderUpdateDto> _updateValidator;

    public PurchaseOrderAppService(
        StoreDbContext dbContext,
        ICurrentUser currentUser,
        IValidator<PurchaseOrderCreateDto> createValidator,
        IValidator<PurchaseOrderUpdateDto> updateValidator)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>
    /// 获取采购订单分页列表
    /// </summary>
    public async Task<ApiResponseDto<PagedResponseDto<PurchaseOrderDto>>> GetPagedListAsync(PurchaseOrderQueryDto query)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<PagedResponseDto<PurchaseOrderDto>>.Fail("无法确定当前租户", 401);

        var tenantId = _currentUser.TenantId.Value;
        var queryable = _dbContext.PurchaseOrders
            .Where(p => p.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(query.OrderNo))
            queryable = queryable.Where(p => p.OrderNo.Contains(query.OrderNo));
        if (query.SupplierId.HasValue)
            queryable = queryable.Where(p => p.SupplierId == query.SupplierId.Value);
        if (query.Status.HasValue)
            queryable = queryable.Where(p => p.Status == query.Status.Value);
        if (query.PurchaseType.HasValue)
            queryable = queryable.Where(p => p.PurchaseType == query.PurchaseType.Value);

        var total = await queryable.CountAsync();
        var items = await queryable
            .OrderByDescending(p => p.CreatedTime)
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var result = new PagedResponseDto<PurchaseOrderDto>
        {
            List = items.Adapt<List<PurchaseOrderDto>>(),
            Total = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
        return ApiResponseDto<PagedResponseDto<PurchaseOrderDto>>.Ok(result);
    }

    /// <summary>
    /// 根据ID获取采购订单详情
    /// </summary>
    public async Task<ApiResponseDto<PurchaseOrderDto?>> GetByIdAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<PurchaseOrderDto?>.Fail("无法确定当前租户", 401);

        var entity = await _dbContext.PurchaseOrders
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == _currentUser.TenantId.Value);
        if (entity == null)
            return ApiResponseDto<PurchaseOrderDto?>.Fail("采购订单不存在", 404);
        return ApiResponseDto<PurchaseOrderDto?>.Ok(entity.Adapt<PurchaseOrderDto>());
    }

    /// <summary>
    /// 创建采购订单（创建即入库，联动库存：创建批次、更新汇总、记录流水）
    /// </summary>
    public async Task<ApiResponseDto<PurchaseOrderDto>> CreateAsync(PurchaseOrderCreateDto dto)
    {
        if (!_currentUser.TenantId.HasValue || !_currentUser.StoreId.HasValue)
            return ApiResponseDto<PurchaseOrderDto>.Fail("无法确定当前租户或门店", 401);

        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<PurchaseOrderDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var tenantCode = _currentUser.TenantCode ?? string.Empty;
        var storeId = _currentUser.StoreId.Value;

        // 采购单号格式校验：必须为 8 位有效日期格式 YYYYMMDD（如 20260718）
        if (!PurchaseOrderNoValidator.IsValid(dto.OrderNo))
            return ApiResponseDto<PurchaseOrderDto>.Fail("采购单号必须为 8 位日期格式 YYYYMMDD（如 20260718）", 400);

        var orderNoExists = await _dbContext.PurchaseOrders
            .AnyAsync(p => p.OrderNo == dto.OrderNo && p.TenantId == tenantId);
        if (orderNoExists)
            return ApiResponseDto<PurchaseOrderDto>.Fail($"采购单号 {dto.OrderNo} 已存在", 400);

        if (dto.Items == null || !dto.Items.Any())
            return ApiResponseDto<PurchaseOrderDto>.Fail("采购明细不能为空", 400);

        var now = DateTime.Now;

        // 映射实体并设置审计字段
        var entity = dto.Adapt<PurchaseOrderEntity>();
        entity.TenantId = tenantId;
        entity.TenantCode = tenantCode;
        entity.CreatedTime = now;
        entity.Status = 3; // 已入库：创建即入库

        // 设置明细审计字段，BatchNo 由 BatchNoGenerator 统一生成（D2 格式 + 唯一性校验）
        for (var i = 0; i < entity.OrderItems.Count; i++)
        {
            var item = entity.OrderItems[i];
            item.TenantId = tenantId;
            item.TenantCode = tenantCode;
            item.StoreId = storeId;
            item.CreatedTime = now;
            item.BatchNo = await BatchNoGenerator.GenerateForPurchaseOrderAsync(
                _dbContext, tenantId, entity.OrderNo, i + 1);

            // 过期日期计算：若录入"生产日期+保质期天数"，自动计算过期日期
            if (item.ProductionDate.HasValue && item.ShelfLifeDays.HasValue)
            {
                item.ExpirationDate = item.ProductionDate.Value.AddDays(item.ShelfLifeDays.Value);
            }
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            _dbContext.PurchaseOrders.Add(entity);
            await _dbContext.SaveChangesAsync();

            // 联动库存：为每个明细创建批次、更新汇总、记录流水
            foreach (var item in entity.OrderItems)
            {
                var batch = new InventoryBatch
                {
                    ProductId = item.ProductId,
                    BatchNo = item.BatchNo!,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    ProductionDate = item.ProductionDate,
                    ShelfLifeDays = item.ShelfLifeDays,
                    ExpirationDate = item.ExpirationDate,
                    PurchaseDate = dto.OrderDate,
                    Status = 1, // 在库
                    TenantId = tenantId,
                    TenantCode = tenantCode,
                    StoreId = storeId,
                    CreatedTime = now
                };
                _dbContext.InventoryBatches.Add(batch);

                var inventory = await _dbContext.Inventories
                    .FirstOrDefaultAsync(inv => inv.ProductId == item.ProductId && inv.TenantId == tenantId && inv.StoreId == storeId);
                var beforeQty = inventory?.Quantity ?? 0;
                if (inventory == null)
                {
                    inventory = new Inventory
                    {
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        AlertQuantity = 0,
                        TenantId = tenantId,
                        TenantCode = tenantCode,
                        StoreId = storeId,
                        CreatedTime = now
                    };
                    _dbContext.Inventories.Add(inventory);
                }
                else
                {
                    inventory.Quantity += item.Quantity;
                    inventory.UpdatedTime = now;
                }

                var log = new InventoryLog
                {
                    ProductId = item.ProductId,
                    Type = 1, // 入库
                    SourceType = InventoryLogSourceTypes.PurchaseInbound, // 采购入库
                    SupplierId = dto.SupplierId,
                    UnitPrice = item.UnitPrice,
                    Quantity = item.Quantity,
                    BeforeQuantity = beforeQty,
                    AfterQuantity = beforeQty + item.Quantity,
                    BatchNo = item.BatchNo,
                    ExpirationDate = item.ExpirationDate,
                    RelatedId = entity.Id,
                    Remark = $"采购入库-{dto.OrderNo}",
                    TenantId = tenantId,
                    TenantCode = tenantCode,
                    StoreId = storeId,
                    CreatedTime = now
                };
                _dbContext.InventoryLogs.Add(log);

                // 更新商品上次采购价（供下次采购自动带出）
                var product = await _dbContext.Products
                    .FirstOrDefaultAsync(p => p.Id == item.ProductId && p.TenantId == tenantId);
                if (product != null)
                {
                    product.LastPurchasePrice = item.UnitPrice;
                    product.UpdatedTime = now;
                }
            }

            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
            return ApiResponseDto<PurchaseOrderDto>.Ok(entity.Adapt<PurchaseOrderDto>(), "创建成功");
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// 更新采购订单
    /// </summary>
    public async Task<ApiResponseDto<PurchaseOrderDto>> UpdateAsync(PurchaseOrderUpdateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<PurchaseOrderDto>.Fail("无法确定当前租户", 401);

        var validation = await _updateValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<PurchaseOrderDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var entity = await _dbContext.PurchaseOrders
            .FirstOrDefaultAsync(p => p.Id == dto.Id && p.TenantId == tenantId);
        if (entity == null)
            return ApiResponseDto<PurchaseOrderDto>.Fail("采购订单不存在", 404);

        // 单号变更时检查唯一性
        if (entity.OrderNo != dto.OrderNo)
        {
            var orderNoExists = await _dbContext.PurchaseOrders
                .AnyAsync(p => p.OrderNo == dto.OrderNo && p.TenantId == tenantId && p.Id != dto.Id);
            if (orderNoExists)
                return ApiResponseDto<PurchaseOrderDto>.Fail($"采购单号 {dto.OrderNo} 已存在", 400);
        }

        entity.OrderNo = dto.OrderNo;
        entity.SupplierId = dto.SupplierId;
        entity.OrderDate = dto.OrderDate;
        entity.TotalAmount = dto.TotalAmount;
        entity.Status = dto.Status;
        entity.PurchaseType = dto.PurchaseType;
        entity.OperatorId = dto.OperatorId;
        entity.Remark = dto.Remark;
        entity.UpdatedTime = DateTime.Now;

        await _dbContext.SaveChangesAsync();
        return ApiResponseDto<PurchaseOrderDto>.Ok(entity.Adapt<PurchaseOrderDto>(), "更新成功");
    }

    /// <summary>
    /// 删除采购订单
    /// </summary>
    public async Task<ApiResponseDto> DeleteAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("无法确定当前租户", 401);

        var entity = await _dbContext.PurchaseOrders
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == _currentUser.TenantId.Value);
        if (entity == null)
            return ApiResponseDto.Fail("采购订单不存在", 404);

        _dbContext.PurchaseOrders.Remove(entity);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, "删除成功");
    }

    /// <summary>
    /// 批量删除采购订单
    /// </summary>
    public async Task<ApiResponseDto> BatchDeleteAsync(List<long> ids)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("无法确定当前租户", 401);
        if (ids == null || !ids.Any())
            return ApiResponseDto.Fail("请选择要删除的数据", 400);

        var entities = await _dbContext.PurchaseOrders
            .Where(p => ids.Contains(p.Id) && p.TenantId == _currentUser.TenantId.Value)
            .ToListAsync();

        _dbContext.PurchaseOrders.RemoveRange(entities);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, $"成功删除 {entities.Count} 条数据");
    }
}
