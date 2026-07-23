using Mapster;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Bms.BuildingBlocks.Abstractions.Security;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.TreatmentCards;
using Bms.Store.Domain.Entities;
using TreatmentCardVerifyEntity = Bms.Store.Domain.Entities.TreatmentCardVerify;
using TreatmentCardVerifyItemEntity = Bms.Store.Domain.Entities.TreatmentCardVerifyItem;
using Bms.Store.Infrastructure;

namespace Bms.Store.Application.Services;

/// <summary>
/// 疗程卡核销记录应用服务实现
/// </summary>
public class TreatmentCardVerifyAppService : ITreatmentCardVerifyAppService
{
    private readonly StoreDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<TreatmentCardVerifyCreateDto> _createValidator;
    private readonly IValidator<TreatmentCardVerifyUpdateDto> _updateValidator;

    public TreatmentCardVerifyAppService(
        StoreDbContext dbContext,
        ICurrentUser currentUser,
        IValidator<TreatmentCardVerifyCreateDto> createValidator,
        IValidator<TreatmentCardVerifyUpdateDto> updateValidator)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>
    /// 获取疗程卡核销记录分页列表
    /// </summary>
    public async Task<ApiResponseDto<PagedResponseDto<TreatmentCardVerifyDto>>> GetPagedListAsync(TreatmentCardVerifyQueryDto query)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<PagedResponseDto<TreatmentCardVerifyDto>>.Fail("无法确定当前租户", 401);

        var tenantId = _currentUser.TenantId.Value;
        var queryable = _dbContext.TreatmentCardVerifies
            .Where(v => v.TenantId == tenantId);

        if (query.CardSaleId.HasValue)
            queryable = queryable.Where(v => v.CardSaleId == query.CardSaleId.Value);
        if (query.VerifyProductId.HasValue)
            queryable = queryable.Where(v => v.Items.Any(i => i.ProductId == query.VerifyProductId.Value));

        var total = await queryable.CountAsync();
        var items = await queryable
            .OrderByDescending(v => v.CreatedTime)
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var result = new PagedResponseDto<TreatmentCardVerifyDto>
        {
            List = items.Adapt<List<TreatmentCardVerifyDto>>(),
            Total = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
        return ApiResponseDto<PagedResponseDto<TreatmentCardVerifyDto>>.Ok(result);
    }

    /// <summary>
    /// 根据ID获取疗程卡核销记录详情
    /// </summary>
    public async Task<ApiResponseDto<TreatmentCardVerifyDto?>> GetByIdAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<TreatmentCardVerifyDto?>.Fail("无法确定当前租户", 401);

        var entity = await _dbContext.TreatmentCardVerifies
            .FirstOrDefaultAsync(v => v.Id == id && v.TenantId == _currentUser.TenantId.Value);
        if (entity == null)
            return ApiResponseDto<TreatmentCardVerifyDto?>.Fail("疗程卡核销记录不存在", 404);
        return ApiResponseDto<TreatmentCardVerifyDto?>.Ok(entity.Adapt<TreatmentCardVerifyDto>());
    }

    /// <summary>
    /// 创建疗程卡核销记录（核销入口，事务包裹）
    /// 支持一次操作核销多个项目（一次到店做多种护理）：
    /// 1) 校验所有项目在疗程卡明细中且次数合法
    /// 2) 按各项目折算单价计算金额，最后一项应用兜底（消除分摊精度误差）
    /// 3) 创建关联订单（OrderType=3，Status=2 已完成，核销不收款）
    /// 4) 创建主单 + N 条明细
    /// 5) 扣减剩余次数、更新累计消费金额
    /// 6) 记录消费（核销不发积分）
    /// </summary>
    public async Task<ApiResponseDto<TreatmentCardVerifyDto>> CreateAsync(TreatmentCardVerifyCreateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<TreatmentCardVerifyDto>.Fail("无法确定当前租户", 401);

        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<TreatmentCardVerifyDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var tenantCode = _currentUser.TenantCode ?? string.Empty;
        var now = DateTime.Now;

        // P-TC-02: 校验核销门店归属当前租户
        // 疗程卡支持租户内跨店核销（合法），但跨租户核销需拒绝，避免伪造 StoreId 越权
        var orderStoreId = dto.StoreId ?? _currentUser.StoreId ?? 0L;
        if (orderStoreId <= 0)
            return ApiResponseDto<TreatmentCardVerifyDto>.Fail("未指定核销门店", 400);
        var storeBelongs = await _dbContext.Stores
            .AnyAsync(s => s.Id == orderStoreId && s.TenantId == tenantId && !s.IsDeleted);
        if (!storeBelongs)
            return ApiResponseDto<TreatmentCardVerifyDto>.Fail("核销门店不属于当前租户", 403);

        // 查询疗程卡销售记录（含项目明细）
        var sale = await _dbContext.TreatmentCardSales
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.Id == dto.CardSaleId && s.TenantId == tenantId);
        if (sale == null)
            return ApiResponseDto<TreatmentCardVerifyDto>.Fail("疗程卡销售记录不存在", 404);

        // 验证疗程卡状态
        if (sale.Status != 1)
            return ApiResponseDto<TreatmentCardVerifyDto>.Fail("疗程卡已用完或已过期，无法核销", 400);
        if (sale.RemainingTimes <= 0)
            return ApiResponseDto<TreatmentCardVerifyDto>.Fail("疗程卡剩余次数为0", 400);
        if (sale.ExpiryDate < now)
            return ApiResponseDto<TreatmentCardVerifyDto>.Fail("疗程卡已过期，无法核销", 400);

        // 校验 Items：每个项目必须在 sale.Items 中，且 VerifyTimes >= 1
        var saleItemDict = sale.Items.ToDictionary(si => si.ProductId, si => si);
        var inputTotalTimes = 0;
        foreach (var item in dto.Items)
        {
            if (item.VerifyTimes < 1)
                return ApiResponseDto<TreatmentCardVerifyDto>.Fail($"项目 #{item.ProductId} 核销次数必须 >= 1", 400);
            if (!saleItemDict.ContainsKey(item.ProductId))
                return ApiResponseDto<TreatmentCardVerifyDto>.Fail($"项目 #{item.ProductId} 不在疗程卡项目明细中", 400);
            inputTotalTimes += item.VerifyTimes;
        }
        if (inputTotalTimes > sale.RemainingTimes)
            return ApiResponseDto<TreatmentCardVerifyDto>.Fail($"本次核销总次数 {inputTotalTimes} 超过疗程卡剩余次数 {sale.RemainingTimes}", 400);

        // 计算每项 SubAmount：仅最后一项应用兜底（消除分摊精度误差）
        // 兜底公式：lastSubAmount = (sale.Amount - sale.TotalConsumedAmount) - sum(previousSubAmounts)
        // 保证 sum(SubAmount) = sale.Amount - sale.TotalConsumedAmount
        var itemCount = dto.Items.Count;
        var computedItems = new List<(TreatmentCardVerifyItemInput Input, TreatmentCardSaleItem SaleItem, decimal SubAmount)>();
        decimal accumulatedSubAmount = 0m;
        for (int i = 0; i < itemCount; i++)
        {
            var input = dto.Items[i];
            var saleItem = saleItemDict[input.ProductId];
            decimal subAmount;
            if (i == itemCount - 1)
            {
                // 最后一项：兜底
                var remainingAmount = sale.Amount - sale.TotalConsumedAmount;
                subAmount = Math.Max(0m, remainingAmount - accumulatedSubAmount);
            }
            else
            {
                subAmount = Math.Round(saleItem.AllocatedUnitPrice * input.VerifyTimes, 2, MidpointRounding.AwayFromZero);
                accumulatedSubAmount += subAmount;
            }
            computedItems.Add((input, saleItem, subAmount));
        }

        // 构造核销明细实体集合（一次性构建，主表冗余字段从明细聚合得出，确保数据一致）
        var verifyItems = computedItems.Select(ci => new TreatmentCardVerifyItem
        {
            ProductId = ci.Input.ProductId,
            VerifyTimes = ci.Input.VerifyTimes,
            AllocatedUnitPrice = ci.SaleItem.AllocatedUnitPrice,
            SubAmount = ci.SubAmount,
            TenantId = tenantId,
            TenantCode = tenantCode,
            CreatedTime = now
        }).ToList();

        // 主表冗余汇总字段 = 明细的聚合（保持单一数据源：明细）
        var totalVerifyAmount = verifyItems.Sum(i => i.SubAmount);
        var totalRequestedTimes = verifyItems.Sum(i => i.VerifyTimes);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            // 创建关联订单（OrderType=3 疗程卡核销，Status=2 已完成，核销不涉及支付）
            // 订单含多个 OrderItem，每个核销项目对应一条
            // 注：orderStoreId 已在事务外完成租户归属校验（见 P-TC-02）
            var orderStoreCode = dto.StoreCode ?? string.Empty;
            var order = new Order
            {
                OrderNo = $"TCV-{now:yyyyMMddHHmmssfff}",
                CustomerId = sale.CustomerId,
                OrderType = 3,
                Status = 2,
                ProductAmount = totalVerifyAmount,
                PaidAmount = 0,
                Points = 0,
                OrderTime = now,
                CompleteTime = now,
                StoreId = orderStoreId,
                StoreCode = orderStoreCode,
                OperatorId = dto.OperatorId,
                Remark = $"疗程卡核销",
                TenantId = tenantId,
                TenantCode = tenantCode,
                CreatedTime = now,
                OrderItems = verifyItems.Select(vi => new OrderItem
                {
                    ProductId = vi.ProductId,
                    Quantity = vi.VerifyTimes,
                    Price = vi.SubAmount / Math.Max(1, vi.VerifyTimes),
                    DiscountedAmount = vi.SubAmount,
                    StoreId = orderStoreId,
                    StoreCode = orderStoreCode,
                    TenantId = tenantId,
                    TenantCode = tenantCode,
                    CreatedTime = now
                }).ToList()
            };
            _dbContext.Orders.Add(order);
            await _dbContext.SaveChangesAsync();

            // P-TC-01: 按 Product.Type 联动扣减库存/BOM 耗材
            // 核销为权责发生制转营收，库存/BOM 扣减为实物消耗，两者在同一事务内完成
            // 扣减失败抛异常触发事务回滚（疗程卡次数不变化）
            // - Type=1（零售）：扣减实物商品库存（FEFO 近效期优先）
            // - Type=2（服务）：扣减 BOM 耗材库存（FEFO 近效期优先）
            // - 其他类型（3=耗材/4=样品/5=赠品）不通过核销扣减
            var verifyProductIds = verifyItems.Select(vi => vi.ProductId).Distinct().ToList();
            var productTypeDict = await _dbContext.Products
                .Where(p => verifyProductIds.Contains(p.Id))
                .Select(p => new { p.Id, p.Type })
                .ToDictionaryAsync(p => p.Id, p => p.Type);
            await DeductInventoryAndBomAsync(order, verifyItems, productTypeDict, now);

            // 创建核销主单（冗余字段从明细聚合得出）
            var verify = new TreatmentCardVerifyEntity
            {
                StoreId = dto.StoreId,
                StoreCode = dto.StoreCode,
                CardSaleId = dto.CardSaleId,
                VerifyAmount = totalVerifyAmount,
                OrderId = order.Id,
                VerifyTimes = totalRequestedTimes,
                VerifyTime = now,
                OperatorId = dto.OperatorId,
                Remark = dto.Remark,
                TenantId = tenantId,
                TenantCode = tenantCode,
                CreatedTime = now,
                Items = verifyItems
            };
            _dbContext.TreatmentCardVerifies.Add(verify);

            // 扣减剩余次数，更新累计消费金额
            sale.RemainingTimes -= totalRequestedTimes;
            sale.TotalConsumedAmount += totalVerifyAmount;
            sale.UpdatedTime = now;
            if (sale.RemainingTimes <= 0)
            {
                sale.Status = 2; // 已用完
            }

            // 记录消费记录（核销不发积分）
            _dbContext.ConsumeLogs.Add(new ConsumeLog
            {
                CustomerId = sale.CustomerId,
                OrderId = order.Id,
                Amount = totalVerifyAmount,
                Points = 0,
                ConsumeTime = now,
                Remark = $"疗程卡核销-{order.OrderNo}",
                TenantId = tenantId,
                TenantCode = tenantCode,
                CreatedTime = now
            });

            // 记录商品销售统计（ProductType=5 疗程卡核销，独立类型，不计入销售排行）
            // 与疗程卡购买（ProductType=4）区分，避免排行重复计算
            // P-DS-05: 核销为权责发生制转营收，不应出现在商品/服务销售 TOP 排行中
            var productIds = computedItems.Select(ci => ci.Input.ProductId).Distinct().ToList();
            var productNames = await _dbContext.Products
                .Where(p => productIds.Contains(p.Id))
                .Select(p => new { p.Id, p.Name })
                .ToDictionaryAsync(p => p.Id, p => p.Name);

            var statDate = now.Date;
            foreach (var ci in computedItems)
            {
                var productName = productNames.GetValueOrDefault(ci.Input.ProductId, string.Empty);
                var existingStat = await _dbContext.ProductSalesStats
                    .FirstOrDefaultAsync(s => s.TenantId == tenantId
                        && s.StoreId == orderStoreId
                        && s.ProductId == ci.Input.ProductId
                        && s.ProductType == 5
                        && s.StatDate == statDate);

                if (existingStat != null)
                {
                    existingStat.SalesCount += ci.Input.VerifyTimes;
                    existingStat.SalesAmount += ci.SubAmount;
                    existingStat.UpdatedTime = now;
                }
                else
                {
                    _dbContext.ProductSalesStats.Add(new ProductSalesStat
                    {
                        StatDate = statDate,
                        StatMonth = $"{now:yyyy-MM}",
                        ProductId = ci.Input.ProductId,
                        ProductName = productName,
                        ProductType = 5,
                        SalesCount = ci.Input.VerifyTimes,
                        SalesAmount = ci.SubAmount,
                        TenantId = tenantId,
                        TenantCode = tenantCode,
                        StoreId = orderStoreId,
                        StoreCode = orderStoreCode,
                        CreatedTime = now
                    });
                }
            }

            // 更新客户最后消费时间
            var customer = await _dbContext.Customers
                .FirstOrDefaultAsync(c => c.Id == sale.CustomerId && c.TenantId == tenantId);
            if (customer != null)
            {
                customer.LastConsumeTime = now;
                customer.UpdatedTime = now;
            }

            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
            return ApiResponseDto<TreatmentCardVerifyDto>.Ok(verify.Adapt<TreatmentCardVerifyDto>(), "核销成功");
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// 更新疗程卡核销记录
    /// </summary>
    public async Task<ApiResponseDto<TreatmentCardVerifyDto>> UpdateAsync(TreatmentCardVerifyUpdateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<TreatmentCardVerifyDto>.Fail("无法确定当前租户", 401);

        var validation = await _updateValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<TreatmentCardVerifyDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var entity = await _dbContext.TreatmentCardVerifies
            .FirstOrDefaultAsync(v => v.Id == dto.Id && v.TenantId == tenantId);
        if (entity == null)
            return ApiResponseDto<TreatmentCardVerifyDto>.Fail("疗程卡核销记录不存在", 404);

        entity.StoreId = dto.StoreId;
        entity.StoreCode = dto.StoreCode;
        entity.CardSaleId = dto.CardSaleId;
        entity.OperatorId = dto.OperatorId;
        entity.Remark = dto.Remark;
        entity.UpdatedTime = DateTime.Now;

        await _dbContext.SaveChangesAsync();
        return ApiResponseDto<TreatmentCardVerifyDto>.Ok(entity.Adapt<TreatmentCardVerifyDto>(), "更新成功");
    }

    /// <summary>
    /// P-TC-01: 按 Product.Type 联动扣减库存/BOM 耗材
    /// 核销为权责发生制转营收，库存/BOM 扣减为实物消耗，两者在同一事务内完成
    /// - Type=1（零售）：扣减实物商品库存（FEFO 近效期优先）
    /// - Type=2（服务）：扣减 BOM 耗材库存（FEFO 近效期优先）
    /// - 其他类型（3=耗材/4=样品/5=赠品）不通过核销扣减
    /// </summary>
    /// <param name="order">核销关联订单（含 OrderItems，与 verifyItems 按索引一一对应）</param>
    /// <param name="verifyItems">核销项目明细</param>
    /// <param name="productTypeDict">ProductId -> Product.Type 映射</param>
    /// <param name="now">统一时间戳</param>
    private async Task DeductInventoryAndBomAsync(
        Order order,
        List<TreatmentCardVerifyItem> verifyItems,
        Dictionary<long, int> productTypeDict,
        DateTime now)
    {
        for (var i = 0; i < verifyItems.Count; i++)
        {
            var verifyItem = verifyItems[i];
            var orderItem = order.OrderItems[i];
            var productType = productTypeDict.GetValueOrDefault(verifyItem.ProductId, 0);

            switch (productType)
            {
                case 1: // 零售商品：扣减实物库存（FEFO）
                    await DeductRetailInventoryForVerifyAsync(order, orderItem, verifyItem.VerifyTimes, now);
                    break;
                case 2: // 服务项目：扣减 BOM 耗材（FEFO）
                    await DeductServiceBomForVerifyAsync(order, orderItem, verifyItem.VerifyTimes, now);
                    break;
                // 其他类型（3=耗材/4=样品/5=赠品）不通过核销扣减
            }
        }
    }

    /// <summary>
    /// 零售商品核销出库：按 FEFO（近效期优先）自动扣减批次库存
    /// 库存不足抛异常，由 CreateAsync 捕获触发事务回滚（疗程卡次数不变化）
    /// </summary>
    private async Task DeductRetailInventoryForVerifyAsync(
        Order order, OrderItem orderItem, int needQty, DateTime now)
    {
        var remaining = (decimal)needQty;

        var batches = await _dbContext.InventoryBatches
            .Where(b => b.ProductId == orderItem.ProductId
                && b.Status == 1
                && b.Quantity > 0
                && b.TenantId == order.TenantId
                && b.StoreId == order.StoreId)
            .OrderBy(b => b.ExpirationDate.HasValue ? 0 : 1)  // null 批次排末尾
            .ThenBy(b => b.ExpirationDate)                    // FEFO: 近效期优先
            .ThenBy(b => b.PurchaseDate)                      // 同效期 FIFO
            .ThenBy(b => b.CreatedTime)                       // null 批次按 CreatedTime 升序，兜底稳定排序
            .ToListAsync();

        foreach (var batch in batches)
        {
            if (remaining <= 0) break;

            var deduct = Math.Min(batch.Quantity, remaining);
            var beforeQty = batch.Quantity;

            // 扣减批次
            batch.Quantity -= deduct;
            batch.UpdatedTime = now;
            if (batch.Quantity <= 0) batch.Status = 2;

            // 更新库存汇总
            var inventory = await _dbContext.Inventories
                .FirstOrDefaultAsync(inv => inv.ProductId == orderItem.ProductId
                    && inv.TenantId == order.TenantId
                    && inv.StoreId == order.StoreId);
            if (inventory != null)
            {
                inventory.Quantity -= deduct;
                inventory.UpdatedTime = now;
            }

            // 记录库存流水（Type=2 出库）
            _dbContext.InventoryLogs.Add(new InventoryLog
            {
                ProductId = orderItem.ProductId,
                Type = 2,
                SourceType = InventoryLogSourceTypes.TreatmentCardOutbound,
                UnitPrice = batch.UnitPrice,
                Quantity = -deduct,
                BeforeQuantity = beforeQty,
                AfterQuantity = batch.Quantity,
                BatchNo = batch.BatchNo,
                ExpirationDate = batch.ExpirationDate,
                RelatedId = order.Id,
                Remark = $"疗程卡核销出库-{order.OrderNo}",
                TenantId = order.TenantId,
                TenantCode = order.TenantCode,
                StoreId = order.StoreId,
                StoreCode = order.StoreCode,
                CreatedTime = now
            });

            // 记录订单明细批次扣减（效期追溯和统计的数据源）
            _dbContext.OrderItemBatches.Add(new OrderItemBatch
            {
                OrderItemId = orderItem.Id,
                OrderId = order.Id,
                ProductId = orderItem.ProductId,
                BatchId = batch.Id,
                BatchNo = batch.BatchNo,
                ExpirationDate = batch.ExpirationDate,
                UnitPrice = batch.UnitPrice,
                Quantity = deduct,
                CostAmount = deduct * batch.UnitPrice,
                RefundedQuantity = 0m,
                TenantId = order.TenantId,
                TenantCode = order.TenantCode,
                StoreId = order.StoreId,
                StoreCode = order.StoreCode,
                CreatedTime = now
            });

            remaining -= deduct;
        }

        if (remaining > 0)
            throw new InvalidOperationException($"商品(ID:{orderItem.ProductId})库存不足，还需 {remaining}");
    }

    /// <summary>
    /// 服务项目核销出库：按 BOM 计算耗材需求，FEFO（近效期优先）自动选择批次扣减
    /// 耗材库存不足抛异常，由 CreateAsync 捕获触发事务回滚
    /// </summary>
    private async Task DeductServiceBomForVerifyAsync(
        Order order, OrderItem orderItem, int verifyTimes, DateTime now)
    {
        // 查找服务项目的 BOM
        var boms = await _dbContext.ServiceBoms
            .Where(b => b.ServiceProductId == orderItem.ProductId && b.TenantId == order.TenantId)
            .ToListAsync();

        if (!boms.Any()) return;

        foreach (var bom in boms)
        {
            // 需要扣减的数量 = BOM 单次消耗量 × 核销次数
            var needQty = bom.Quantity * verifyTimes;

            // FEFO: 近效期优先，null 批次排末尾按 CreatedTime 升序
            var batches = await _dbContext.InventoryBatches
                .Where(b => b.ProductId == bom.ConsumableProductId
                    && b.Status == 1
                    && b.Quantity > 0
                    && b.TenantId == order.TenantId
                    && b.StoreId == order.StoreId)
                .OrderBy(b => b.ExpirationDate.HasValue ? 0 : 1)  // null 批次排末尾
                .ThenBy(b => b.ExpirationDate)                    // 近效期优先
                .ThenBy(b => b.PurchaseDate)                      // 同效期 FIFO
                .ThenBy(b => b.CreatedTime)                       // null 批次按 CreatedTime 升序，兜底稳定排序
                .ToListAsync();

            var remaining = needQty;
            foreach (var batch in batches)
            {
                if (remaining <= 0) break;
                var deduct = Math.Min(batch.Quantity, remaining);

                var beforeQty = batch.Quantity;
                batch.Quantity -= deduct;
                batch.UpdatedTime = now;
                if (batch.Quantity <= 0) batch.Status = 2;

                // 更新库存汇总
                var inventory = await _dbContext.Inventories
                    .FirstOrDefaultAsync(inv => inv.ProductId == bom.ConsumableProductId
                        && inv.TenantId == order.TenantId
                        && inv.StoreId == order.StoreId);
                if (inventory != null)
                {
                    inventory.Quantity -= deduct;
                    inventory.UpdatedTime = now;
                }

                // 记录流水
                _dbContext.InventoryLogs.Add(new InventoryLog
                {
                    ProductId = bom.ConsumableProductId,
                    Type = 2,
                    SourceType = InventoryLogSourceTypes.TreatmentCardOutbound,
                    UnitPrice = batch.UnitPrice,
                    Quantity = -deduct,
                    BeforeQuantity = beforeQty,
                    AfterQuantity = batch.Quantity,
                    BatchNo = batch.BatchNo,
                    ExpirationDate = batch.ExpirationDate,
                    RelatedId = order.Id,
                    Remark = $"疗程卡核销耗材出库-{order.OrderNo}",
                    TenantId = order.TenantId,
                    TenantCode = order.TenantCode,
                    StoreId = order.StoreId,
                    StoreCode = order.StoreCode,
                    CreatedTime = now
                });

                // 记录订单明细批次扣减（ProductId 记录耗材ID，便于按耗材统计效期消耗）
                _dbContext.OrderItemBatches.Add(new OrderItemBatch
                {
                    OrderItemId = orderItem.Id,
                    OrderId = order.Id,
                    ProductId = bom.ConsumableProductId,
                    BatchId = batch.Id,
                    BatchNo = batch.BatchNo,
                    ExpirationDate = batch.ExpirationDate,
                    UnitPrice = batch.UnitPrice,
                    Quantity = deduct,
                    CostAmount = deduct * batch.UnitPrice,
                    RefundedQuantity = 0m,
                    TenantId = order.TenantId,
                    TenantCode = order.TenantCode,
                    StoreId = order.StoreId,
                    StoreCode = order.StoreCode,
                    CreatedTime = now
                });

                remaining -= deduct;
            }

            if (remaining > 0)
                throw new InvalidOperationException($"耗材(ID:{bom.ConsumableProductId})库存不足，还需 {remaining}");
        }
    }
}
