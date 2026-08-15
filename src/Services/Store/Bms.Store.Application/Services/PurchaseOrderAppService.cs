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
/// 采购订单创建即入库，单据不可变，仅支持查询与创建
/// </summary>
public class PurchaseOrderAppService : IPurchaseOrderAppService
{
    private readonly StoreDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<PurchaseOrderCreateDto> _createValidator;
    private readonly IInventoryAlertAppService _alertAppService;

    public PurchaseOrderAppService(
        StoreDbContext dbContext,
        ICurrentUser currentUser,
        IValidator<PurchaseOrderCreateDto> createValidator,
        IInventoryAlertAppService alertAppService)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _alertAppService = alertAppService;
    }

    /// <summary>
    /// 获取采购订单分页列表
    /// SupplierId 过滤按明细供应商子查询，支持一个订单含多供应商的场景
    /// </summary>
    public async Task<ApiResponseDto<PagedResponseDto<PurchaseOrderDto>>> GetPagedListAsync(PurchaseOrderQueryDto query)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<PagedResponseDto<PurchaseOrderDto>>.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId ?? 0;
        var queryable = _dbContext.PurchaseOrders
            .Where(p => p.TenantId == tenantId && p.StoreId == storeId);

        if (!string.IsNullOrWhiteSpace(query.OrderNo))
            queryable = queryable.Where(p => p.OrderNo.Contains(query.OrderNo));
        if (query.SupplierId.HasValue)
            queryable = queryable.Where(p => p.OrderItems.Any(i => i.SupplierId == query.SupplierId.Value));
        if (query.ProductId.HasValue)
            queryable = queryable.Where(p => p.OrderItems.Any(i => i.ProductId == query.ProductId.Value));
        if (query.PurchaseType.HasValue)
            queryable = queryable.Where(p => p.PurchaseType == query.PurchaseType.Value);
        if (query.OrderDateStart.HasValue)
            queryable = queryable.Where(p => p.OrderDate >= query.OrderDateStart.Value);
        if (query.OrderDateEnd.HasValue)
            // EndDate 含当日：用次日0点作为上界（exclusive）避免时间部分漏掉当天数据
            queryable = queryable.Where(p => p.OrderDate < query.OrderDateEnd.Value.AddDays(1));

        var total = await queryable.CountAsync();
        var items = await queryable
            .Include(p => p.OrderItems)
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
    /// 根据ID获取采购订单详情（含明细）
    /// </summary>
    public async Task<ApiResponseDto<PurchaseOrderDto?>> GetByIdAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<PurchaseOrderDto?>.Fail("登录状态异常，请重新登录", 401);

        var entity = await _dbContext.PurchaseOrders
            .Include(p => p.OrderItems)
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == _currentUser.TenantId.Value && p.StoreId == (_currentUser.StoreId ?? 0));
        if (entity == null)
            return ApiResponseDto<PurchaseOrderDto?>.Fail("采购订单不存在", 404);
        return ApiResponseDto<PurchaseOrderDto?>.Ok(entity.Adapt<PurchaseOrderDto>());
    }

    /// <summary>
    /// 创建采购订单（创建即入库，联动库存：创建批次、更新汇总、记录流水）
    /// 采购单号由后端自动生成（PO{yyyyMMdd}{序号}），批次号统一格式（{yyyyMMdd}-{序号}）
    /// </summary>
    public async Task<ApiResponseDto<PurchaseOrderDto>> CreateAsync(PurchaseOrderCreateDto dto)
    {
        if (!_currentUser.TenantId.HasValue || !_currentUser.StoreId.HasValue)
            return ApiResponseDto<PurchaseOrderDto>.Fail("登录状态异常，请重新登录", 401);

        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<PurchaseOrderDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var tenantCode = _currentUser.TenantCode ?? string.Empty;
        var storeId = _currentUser.StoreId.Value;

        if (dto.Items == null || !dto.Items.Any())
            return ApiResponseDto<PurchaseOrderDto>.Fail("采购明细不能为空", 400);

        var now = DateTime.Now;

        // 映射实体并设置审计字段
        var entity = dto.Adapt<PurchaseOrderEntity>();
        entity.TenantId = tenantId;
        entity.TenantCode = tenantCode;
        entity.StoreId = storeId;
        entity.CreatedTime = now;
        // 采购单创建即入库，Status 由实体默认值 1（已入库）控制，无需显式赋值
        // 采购单号在事务内（顾问锁保护下）生成，避免并发重复
        // 记录操作人员（采购单创建即入库，OperatorId 标识谁执行的此次采购）
        entity.OperatorId = _currentUser.UserId;
        entity.OperatorName = _currentUser.RealName ?? _currentUser.UserName;

        // 设置明细审计字段，SupplierId 取自 DTO 明细
        // BatchNo 在事务内（顾问锁保护下）统一生成，避免并发重复
        for (var i = 0; i < entity.OrderItems.Count; i++)
        {
            var item = entity.OrderItems[i];
            item.TenantId = tenantId;
            item.TenantCode = tenantCode;
            item.StoreId = storeId;
            item.CreatedTime = now;
            item.SupplierId = dto.Items[i].SupplierId;
            // 小计金额 = 数量 × 单价，由后端统一计算避免前端传错
            item.TotalPrice = item.Quantity * item.UnitPrice;

            // 过期日期计算：若录入"生产日期+保质期天数"，自动计算过期日期
            if (item.ProductionDate.HasValue && item.ShelfLifeDays.HasValue)
            {
                item.ExpirationDate = item.ProductionDate.Value.AddDays(item.ShelfLifeDays.Value);
            }
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            // 事务级顾问锁：按 (租户, 门店, 采购日期) 串行化并发请求
            // OrderNo/BatchNo 采用"查max/count+1"生成模式，并发下需串行化避免唯一约束冲突
            var lockKey = PurchaseOrderNoGenerator.BuildLockKey(tenantId, storeId, dto.OrderDate);
            await _dbContext.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock({0})", lockKey);

            // 在锁保护下生成单号
            entity.OrderNo = await PurchaseOrderNoGenerator.GenerateAsync(_dbContext, tenantId, storeId, dto.OrderDate);
            for (var i = 0; i < entity.OrderItems.Count; i++)
            {
                entity.OrderItems[i].BatchNo = await BatchNoGenerator.GenerateAsync(_dbContext, tenantId, storeId, dto.OrderDate, i);
            }

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
                    SupplierId = item.SupplierId,
                    UnitPrice = item.UnitPrice,
                    Quantity = item.Quantity,
                    BeforeQuantity = beforeQty,
                    AfterQuantity = beforeQty + item.Quantity,
                    BatchNo = item.BatchNo,
                    ExpirationDate = item.ExpirationDate,
                    RelatedId = entity.Id,
                    Remark = entity.OrderNo,
                    OperatorId = _currentUser.UserId,
                    OperatorName = _currentUser.RealName ?? _currentUser.UserName,
                    TenantId = tenantId,
                    TenantCode = tenantCode,
                    StoreId = storeId,
                    CreatedTime = now
                };
                _dbContext.InventoryLogs.Add(log);

                // 更新商品上次采购价（供下次采购自动带出）
                var product = await _dbContext.Products
                    .FirstOrDefaultAsync(p => p.Id == item.ProductId && p.TenantId == tenantId && p.StoreId == storeId);
                if (product != null)
                {
                    product.LastPurchasePrice = item.UnitPrice;
                    product.UpdatedTime = now;
                }
            }

            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            // 即时检测预警（失败不影响入库结果，定时任务兜底）
            foreach (var item in entity.OrderItems)
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

            return ApiResponseDto<PurchaseOrderDto>.Ok(entity.Adapt<PurchaseOrderDto>(), "创建成功");
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
