using Mapster;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Bms.BuildingBlocks.Abstractions.Security;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Orders;
using Bms.Store.Application.Services.Resources;
using Bms.Store.Domain.Entities;
using Bms.Store.Domain.Constants;
using OrderEntity = Bms.Store.Domain.Entities.Order;
using AppointmentEntity = Bms.Store.Domain.Entities.Appointment;
using Bms.Store.Infrastructure;

namespace Bms.Store.Application.Services;

/// <summary>
/// 订单应用服务实现
/// </summary>
public class OrderAppService : IOrderAppService
{
    private readonly StoreDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<OrderCreateDto> _createValidator;
    private readonly IValidator<RefundRequestDto> _refundValidator;
    private readonly IValidator<OrderCancelDto> _cancelValidator;
    private readonly IInventoryAlertAppService _alertAppService;
    private readonly IResourceConflictCheckService _resourceConflictCheckService;
    private readonly IStoredValueAccountAppService _storedValueAccountAppService;
    private readonly IDailySettlementAppService _dailySettlementAppService;

    public OrderAppService(
        StoreDbContext dbContext,
        ICurrentUser currentUser,
        IValidator<OrderCreateDto> createValidator,
        IValidator<RefundRequestDto> refundValidator,
        IValidator<OrderCancelDto> cancelValidator,
        IInventoryAlertAppService alertAppService,
        IResourceConflictCheckService resourceConflictCheckService,
        IStoredValueAccountAppService storedValueAccountAppService,
        IDailySettlementAppService dailySettlementAppService)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _refundValidator = refundValidator;
        _cancelValidator = cancelValidator;
        _alertAppService = alertAppService;
        _resourceConflictCheckService = resourceConflictCheckService;
        _storedValueAccountAppService = storedValueAccountAppService;
        _dailySettlementAppService = dailySettlementAppService;
    }

    /// <summary>
    /// 获取订单分页列表
    /// </summary>
    public async Task<ApiResponseDto<PagedResponseDto<OrderDto>>> GetPagedListAsync(OrderQueryDto query)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<PagedResponseDto<OrderDto>>.Fail("无法确定当前租户", 401);

        var tenantId = _currentUser.TenantId.Value;
        var queryable = _dbContext.Orders
            .Where(o => o.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(query.OrderNo))
            queryable = queryable.Where(o => o.OrderNo.Contains(query.OrderNo));
        if (query.CustomerId.HasValue)
            queryable = queryable.Where(o => o.CustomerId == query.CustomerId.Value);
        if (query.OrderType.HasValue)
            queryable = queryable.Where(o => o.OrderType == query.OrderType.Value);
        if (query.Status.HasValue)
            queryable = queryable.Where(o => o.Status == query.Status.Value);
        if (query.PayMethod.HasValue)
            queryable = queryable.Where(o => o.PayMethod == query.PayMethod.Value);
        if (query.BackfillStatus.HasValue)
            queryable = queryable.Where(o => o.BackfillStatus == query.BackfillStatus.Value);
        if (query.StartDate.HasValue)
            queryable = queryable.Where(o => o.OrderTime >= query.StartDate.Value);
        if (query.EndDate.HasValue)
            queryable = queryable.Where(o => o.OrderTime < query.EndDate.Value.AddDays(1));

        var total = await queryable.CountAsync();
        var items = await queryable
            .OrderByDescending(o => o.CreatedTime)
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var result = new PagedResponseDto<OrderDto>
        {
            List = items.Adapt<List<OrderDto>>(),
            Total = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
        return ApiResponseDto<PagedResponseDto<OrderDto>>.Ok(result);
    }

    /// <summary>
    /// 获取今日待补录订单列表（为站内信功能预留数据源）
    /// 查询条件：当前租户下 BackfillStatus=1 且 OrderTime <= 今日的订单
    /// </summary>
    public async Task<ApiResponseDto<List<OrderDto>>> GetTodayBackfillOrdersAsync()
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<List<OrderDto>>.Fail("无法确定当前租户", 401);

        var tenantId = _currentUser.TenantId.Value;
        var tomorrow = DateTime.Today.AddDays(1);

        var orders = await _dbContext.Orders
            .Where(o => o.TenantId == tenantId
                && o.BackfillStatus == 1
                && o.OrderTime < tomorrow)
            .OrderByDescending(o => o.OrderTime)
            .Take(100)
            .ToListAsync();

        return ApiResponseDto<List<OrderDto>>.Ok(orders.Adapt<List<OrderDto>>());
    }

    /// <summary>
    /// 根据ID获取订单详情（含明细列表，并关联填充技师姓名）
    /// </summary>
    public async Task<ApiResponseDto<OrderDto?>> GetByIdAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<OrderDto?>.Fail("无法确定当前租户", 401);

        var entity = await _dbContext.Orders
            .Include(o => o.OrderItems).ThenInclude(oi => oi.Batches)
            .FirstOrDefaultAsync(o => o.Id == id && o.TenantId == _currentUser.TenantId.Value);
        if (entity == null)
            return ApiResponseDto<OrderDto?>.Fail("订单不存在", 404);

        var dto = entity.Adapt<OrderDto>();

        // 关联查询技师姓名填充到明细
        var technicianIds = dto.Items
            .Where(i => i.TechnicianId.HasValue)
            .Select(i => i.TechnicianId!.Value)
            .Distinct()
            .ToList();
        if (technicianIds.Any())
        {
            var technicianNames = await _dbContext.Technicians
                .Where(t => technicianIds.Contains(t.Id))
                .ToDictionaryAsync(t => t.Id, t => t.Name);
            foreach (var item in dto.Items.Where(i => i.TechnicianId.HasValue))
            {
                if (technicianNames.TryGetValue(item.TechnicianId!.Value, out var name))
                    item.TechnicianName = name;
            }
        }

        return ApiResponseDto<OrderDto?>.Ok(dto);
    }

    /// <summary>
    /// 创建订单（事务包裹，按 OrderType 联动库存/疗程卡，按 PayMethod 联动储值）
    /// OrderType=1零售: 扣减实物商品库存（按效期选择批次）
    /// OrderType=2服务: 扣减BOM耗材库存（FEFO自动选择）
    /// OrderType=3疗程卡核销: 核销疗程卡+附加零售商品出库
    /// PayMethod=5储值支付: 扣减储值余额（先实收后赠送，不发积分）
    /// 积分规则: PayMethod≠5且OrderType≠3时按PointsRate计算，Floor取整
    /// </summary>
    public async Task<ApiResponseDto<OrderDto>> CreateAsync(OrderCreateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<OrderDto>.Fail("无法确定当前租户", 401);

        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<OrderDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var tenantCode = _currentUser.TenantCode ?? string.Empty;

        // 检查订单号唯一性
        var noExists = await _dbContext.Orders
            .AnyAsync(o => o.OrderNo == dto.OrderNo && o.TenantId == tenantId);
        if (noExists)
            return ApiResponseDto<OrderDto>.Fail($"订单号 {dto.OrderNo} 已存在", 400);

        if (dto.Items == null || !dto.Items.Any())
            return ApiResponseDto<OrderDto>.Fail("订单明细不能为空", 400);

        // 疗程卡核销订单（OrderType=3）必须通过 TreatmentCardVerifyAppService.CreateAsync 创建
        // 该方法负责：① 创建核销订单 ② 扣减疗程卡次数 ③ 按 Product.Type 联动扣库存/BOM（见 P-TC-01）
        // 此处禁止 OrderType=3 的入口，避免遗漏库存/BOM 扣减
        if (dto.OrderType == 3)
            return ApiResponseDto<OrderDto>.Fail("疗程卡核销请通过核销接口创建", 400);

        // 不可销售品项中，仅 Type=4 样品仍禁止下单（必须走样品领用流程）
        // Type=5 赠品允许进入订单，走赠品出库路径（DeductSampleGiftOutAsync，详见第 1 步库存扣减）
        // 前端已保证赠品 Price=0、ExpirationDates 非空，后端不做这些业务校验（V4 方案）
        var orderProductIds = dto.Items.Select(i => i.ProductId).Distinct().ToList();
        var productTypeMap = await _dbContext.Products
            .Where(p => orderProductIds.Contains(p.Id) && !p.IsDeleted)
            .ToDictionaryAsync(p => p.Id, p => p.Type);

        var sampleProductIds = productTypeMap
            .Where(kv => kv.Value == 4)
            .Select(kv => kv.Key)
            .ToList();
        if (sampleProductIds.Any())
        {
            var names = string.Join("、", sampleProductIds.Select(id => $"商品ID:{id}"));
            return ApiResponseDto<OrderDto>.Fail($"样品不可销售，请通过样品领用流程操作：{names}", 400);
        }

        // 识别 Type=5 赠品，供后续库存扣减分流使用
        // 赠品走 DeductSampleGiftOutAsync（写 SampleGiftOut + InventoryLog SourceType=GiftOutbound）
        // 非赠品走原 DeductRetailInventoryAsync（销售出库）
        var giftProductIds = productTypeMap
            .Where(kv => kv.Value == 5)
            .Select(kv => kv.Key)
            .ToHashSet();

        var now = DateTime.Now;

        // 预约转订单：从源预约复制 TechnicianId/RoomId/EquipmentId 到 OrderItem（按序号一一对应）
        // 预约创建时已通过 IResourceConflictCheckService 完成占用检测，订单侧不再重复验证
        AppointmentEntity? sourceAppointment = null;
        if (dto.SourceAppointmentId.HasValue)
        {
            sourceAppointment = await _dbContext.Appointments
                .FirstOrDefaultAsync(a => a.Id == dto.SourceAppointmentId.Value && a.TenantId == tenantId);
            if (sourceAppointment == null)
                return ApiResponseDto<OrderDto>.Fail($"源预约 {dto.SourceAppointmentId} 不存在", 400);

            for (var i = 0; i < dto.Items.Count; i++)
            {
                var item = dto.Items[i];
                // OrderItem 未指定技师/房间/设备时，从源预约继承（前端可显式覆盖）
                if (!item.TechnicianId.HasValue)
                {
                    item.TechnicianId = sourceAppointment.TechnicianId;
                    // 同步复制技师来源（1=自有，2=平台），用于报表按技师来源分组统计
                    item.TechnicianSource = sourceAppointment.TechnicianSource;
                }
                if (!item.RoomId.HasValue) item.RoomId = sourceAppointment.RoomId;
                if (!item.EquipmentId.HasValue) item.EquipmentId = sourceAppointment.EquipmentId;
            }
        }

        // 服务订单（OrderType=2）资源冲突检测（技师/房间/设备，跨 Appointment + OrderItem 双向）
        // 预约转订单场景已由源预约保证占用，跳过验证
        if (dto.OrderType == 2 && !dto.SourceAppointmentId.HasValue)
        {
            // 预加载每个 OrderItem 的服务时长（按 ProductId 聚合），用于计算占用结束时间
            var productIds = dto.Items.Select(i => i.ProductId).Distinct().ToList();
            var productDurations = await _dbContext.ServiceProducts
                .Where(sp => productIds.Contains(sp.ProductId))
                .ToDictionaryAsync(sp => sp.ProductId, sp => sp.Duration ?? 0);

            var orderStart = dto.OrderTime == default ? now : dto.OrderTime;

            // 按技师/房间/设备聚合占用区间
            var conflicts = new List<string>();
            foreach (var item in dto.Items)
            {
                if (!productDurations.TryGetValue(item.ProductId, out var duration) || duration <= 0)
                {
                    // 服务项目未配置时长，无法判定占用区间，跳过冲突检测
                    continue;
                }
                var itemEnd = orderStart.AddMinutes(duration);

                var result = await _resourceConflictCheckService.CheckAsync(
                    tenantId, item.TechnicianId, item.RoomId, item.EquipmentId,
                    orderStart, itemEnd);

                if (result.HasAnyConflict)
                {
                    if (result.TechnicianConflict)
                        conflicts.Add($"技师（{result.TechnicianConflictInfo}）");
                    if (result.RoomConflict)
                        conflicts.Add($"房间（{result.RoomConflictInfo}）");
                    if (result.EquipmentConflict)
                        conflicts.Add($"设备（{result.EquipmentConflictInfo}）");
                }
            }
            if (conflicts.Any())
            {
                return ApiResponseDto<OrderDto>.Fail(
                    $"以下资源已被占用，请更换时段或资源：{string.Join("；", conflicts.Distinct())}", 400);
            }
        }

        // 映射实体并设置审计字段
        var entity = dto.Adapt<OrderEntity>();
        entity.TenantId = tenantId;
        entity.TenantCode = tenantCode;
        entity.CreatedTime = now;
        entity.OrderTime = dto.OrderTime == default ? now : dto.OrderTime;
        // OrderType=1零售：立即完成（库存已扣减）
        // OrderType=2服务：进行中（待服务完成后由 Complete 接口改为已完成）
        // 进行中状态用于资源冲突检测 Order.Status = 1 的占用判定
        if (dto.OrderType == 2)
        {
            entity.Status = 1; // 进行中
        }
        else
        {
            entity.Status = 2; // 已完成
            entity.CompleteTime = now;
        }

        // 设置明细审计字段
        foreach (var item in entity.OrderItems)
        {
            item.TenantId = tenantId;
            item.TenantCode = tenantCode;
            item.CreatedTime = now;
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            _dbContext.Orders.Add(entity);
            await _dbContext.SaveChangesAsync();

            // 1. 按 OrderType 联动库存/疗程卡
            //   Type=5 赠品与 OrderType 无关，所有订单类型均支持附赠赠品（V4 方案，P-SG-03）
            //   - OrderType=1 零售：非赠品走销售出库 + 赠品走赠品出库
            //   - OrderType=2 服务：BOM 耗材扣减 + 赠品走赠品出库（BOM 不涉及赠品）
            switch (dto.OrderType)
            {
                case 1: // 零售：扣减实物商品库存 + 赠品出库
                    await DeductRetailInventoryAsync(entity, dto.Items, giftProductIds, now);
                    if (giftProductIds.Any())
                        await DeductSampleGiftOutAsync(entity, dto.Items, giftProductIds, now);
                    break;
                case 2: // 服务：扣减BOM耗材库存 + 赠品出库
                    await DeductServiceBomAsync(entity, now);
                    if (giftProductIds.Any())
                        await DeductSampleGiftOutAsync(entity, dto.Items, giftProductIds, now);
                    break;
            }

            // 2. 按 PayMethod 联动储值扣减/积分扣减/组合支付
            if (dto.PayMethod == 5)
            {
                await DeductStoredValueAsync(entity, entity.PaidAmount, now);
            }
            else if (dto.PayMethod == 6)
            {
                await DeductPointsAsync(entity, entity.PaidAmount, now);
            }
            else if (dto.PayMethod == 7)
            {
                await DeductCombinedPaymentAsync(entity, now);
            }

            // 3. 积分发放（PayMethod=5储值支付、PayMethod=6积分抵扣、OrderType=3疗程卡核销不发积分）
            //    组合支付（PayMethod=7）发积分基数 = CashAmount + StoredValueAmount（排除积分抵扣部分，避免重复/循环发放）
            if (dto.PayMethod != 5 && dto.PayMethod != 6 && dto.OrderType != 3)
            {
                var awardBaseAmount = dto.PayMethod == 7
                    ? (entity.CashAmount ?? 0m) + (entity.StoredValueAmount ?? 0m)
                    : entity.PaidAmount;
                if (awardBaseAmount > 0)
                {
                    await AwardPointsAsync(entity, awardBaseAmount, now);
                }
            }

            // 4. 记录消费记录 + 更新客户累计消费
            await RecordConsumeLogAsync(entity, now);

            // 5. 记录商品销售统计（ProductSalesStat，跳过 Type=5 赠品）
            await RecordProductSalesStatAsync(entity, giftProductIds, now);

            // 6. 补录订单跨日触发原下单日反日结（方案 B，2026-07-19）
            // 补录订单(BackfillStatus=1)下单日早于今日时，自动反日结原下单日并重算
            // 与退款/取消订单保持一致的跨日变动处理口径
            List<DateTime> reversedDates = new();
            if (entity.BackfillStatus == 1 && entity.OrderTime.Date < DateTime.Today)
            {
                var reverseReason = $"补录订单自动反日结：订单 {entity.OrderNo}（实付 {entity.PaidAmount:F2} 元）";
                reversedDates = await ReverseSettlementForOrderReversalAsync(entity, reverseReason, now);
            }

            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            // 库存变动后即时检测低库存预警
            await CheckLowStockAfterInventoryChangeAsync(entity.Id, tenantId, entity.StoreId);

            var message = reversedDates.Any()
                ? $"创建成功，订单下单日 {string.Join("、", reversedDates.Select(d => d.ToString("yyyy-MM-dd")))} 日结已自动反日结/补建，请前往日结管理重新确认"
                : "创建成功";
            return ApiResponseDto<OrderDto>.Ok(entity.Adapt<OrderDto>(), message);
        }
        catch (InvalidOperationException ex) when (ex.Message.StartsWith("INSUFFICIENT_EXPIRY_STOCK"))
        {
            // 选中效期库存不足：回滚事务，返回 409 让前端弹窗让店员决定是否自动补足
            await transaction.RollbackAsync();
            return ApiResponseDto<OrderDto>.Fail(ex.Message, 409);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// 库存变动后即时检测低库存预警（通过 InventoryLog 查找涉及的商品）
    /// </summary>
    private async Task CheckLowStockAfterInventoryChangeAsync(long orderId, long tenantId, long storeId)
    {
        var affectedProductIds = await _dbContext.InventoryLogs
            .Where(l => l.RelatedId == orderId && l.TenantId == tenantId && l.StoreId == storeId)
            .Select(l => l.ProductId)
            .Distinct()
            .ToListAsync();

        foreach (var productId in affectedProductIds)
        {
            try
            {
                await _alertAppService.CheckLowStockAsync(tenantId, storeId, productId);
            }
            catch
            {
                // 预警检测失败不影响主流程，定时任务会兜底
            }
        }
    }

    /// <summary>
    /// 零售出库：遍历 dto.Items 中的非赠品项，调用 DeductItemBatchesAsync 按 ExpirationDates + FEFO 扣减批次库存。
    /// 赠品（Type=5）由 DeductSampleGiftOutAsync 单独处理。
    /// </summary>
    private async Task DeductRetailInventoryAsync(
        OrderEntity order, List<OrderItemCreateDto> dtoItems, HashSet<long> giftProductIds, DateTime now)
    {
        for (var i = 0; i < dtoItems.Count; i++)
        {
            var dtoItem = dtoItems[i];
            if (giftProductIds.Contains(dtoItem.ProductId)) continue;

            var item = order.OrderItems[i];
            await DeductItemBatchesAsync(order, item, dtoItem, now,
                InventoryLogSourceTypes.SalesOutbound, createSampleGiftOut: false);
        }
    }

    /// <summary>
    /// 赠品出库扣减：遍历 dto.Items 中的 Type=5 赠品，调用 DeductItemBatchesAsync 按效期扣减批次库存。
    /// 每个扣减批次创建独立 SampleGiftOut 记录（与 SampleGiftOut 实体的"单批次"设计一致），
    /// 跨批次扣减时创建多条记录，均关联同一 OrderId（由订单创建事务保证原子性，P-SG-03）。
    /// </summary>
    private async Task DeductSampleGiftOutAsync(
        OrderEntity order, List<OrderItemCreateDto> dtoItems, HashSet<long> giftProductIds, DateTime now)
    {
        for (var i = 0; i < dtoItems.Count; i++)
        {
            var dtoItem = dtoItems[i];
            if (!giftProductIds.Contains(dtoItem.ProductId)) continue;

            var item = order.OrderItems[i];
            await DeductItemBatchesAsync(order, item, dtoItem, now,
                InventoryLogSourceTypes.GiftOutbound, createSampleGiftOut: true);
        }
    }

    /// <summary>
    /// 单个 OrderItem 的批次选择与扣减（DeductRetailInventoryAsync 与 DeductSampleGiftOutAsync 共享）。
    /// 按店员选择的效期顺序优先扣减，同效期多批次按 PurchaseDate ASC（FIFO）逐批扣减。
    /// 店员未选效期或允许自动补足时，按 FEFO（近效期优先）自动扣减。
    /// 选中效期不足且 AllowAutoFillBeyondSelection=false 时抛带 INSUFFICIENT_EXPIRY_STOCK 前缀的异常，
    /// 由 CreateAsync 捕获并返回 409，前端弹窗让店员决定是否自动从近效期补足。
    /// 通过 sourceType 区分 InventoryLog.SourceType，通过 createSampleGiftOut 控制是否创建 SampleGiftOut 记录。
    /// </summary>
    private async Task DeductItemBatchesAsync(
        OrderEntity order, OrderItem item, OrderItemCreateDto dtoItem, DateTime now,
        int sourceType, bool createSampleGiftOut)
    {
        var remaining = item.Quantity;
        // 记录已扣减的效期日期（自动补足时排除）；null 表示"无效期限制"批次
        var processedExpirationDates = new List<DateTime?>();

        // 1. 店员选择了效期 -> 按选择顺序扣减
        // 注：ExpirationDates 元素为 DateTime?，null 表示店员选择了"无效期限制"批次
        if (dtoItem.ExpirationDates != null && dtoItem.ExpirationDates.Any())
        {
            foreach (var expirationDate in dtoItem.ExpirationDates)
            {
                if (remaining <= 0) break;

                // 有值匹配同效期批次（按 PurchaseDate FIFO）；null 匹配未设置效期的批次（按 CreatedTime 升序）
                List<InventoryBatch> batches;
                if (expirationDate.HasValue)
                {
                    batches = await _dbContext.InventoryBatches
                        .Where(b => b.ProductId == dtoItem.ProductId
                            && b.ExpirationDate == expirationDate.Value
                            && b.Status == 1
                            && b.Quantity > 0
                            && b.TenantId == order.TenantId
                            && b.StoreId == order.StoreId)
                        .OrderBy(b => b.PurchaseDate) // 同效期 FIFO
                        .ToListAsync();
                }
                else
                {
                    batches = await _dbContext.InventoryBatches
                        .Where(b => b.ProductId == dtoItem.ProductId
                            && !b.ExpirationDate.HasValue
                            && b.Status == 1
                            && b.Quantity > 0
                            && b.TenantId == order.TenantId
                            && b.StoreId == order.StoreId)
                        .OrderBy(b => b.CreatedTime) // 无效期批次按 CreatedTime 升序
                        .ToListAsync();
                }

                remaining = await DeductFromBatchesAsync(order, item, batches, remaining, now, sourceType, createSampleGiftOut);
                processedExpirationDates.Add(expirationDate);
            }
        }

        // 2. 未选效期 OR 允许自动补足 -> 系统按 FEFO 自动扣减（排除已扣减的效期）
        // 注：包含 null 批次，排末尾按 CreatedTime 升序扣减
        if (remaining > 0
            && (dtoItem.ExpirationDates == null || !dtoItem.ExpirationDates.Any() || dtoItem.AllowAutoFillBeyondSelection))
        {
            var fefoQuery = _dbContext.InventoryBatches
                .Where(b => b.ProductId == dtoItem.ProductId
                    && b.Status == 1
                    && b.Quantity > 0
                    && b.TenantId == order.TenantId
                    && b.StoreId == order.StoreId);

            if (processedExpirationDates.Any())
            {
                // 排除已扣减的效期：有值按 Date 匹配，null 单独排除
                var processedDates = processedExpirationDates
                    .Where(d => d.HasValue)
                    .Select(d => d.Value.Date)
                    .ToList();
                var hasProcessedNull = processedExpirationDates.Any(d => !d.HasValue);

                if (processedDates.Any())
                {
                    fefoQuery = fefoQuery.Where(b => !b.ExpirationDate.HasValue
                        || !processedDates.Contains(b.ExpirationDate.Value.Date));
                }
                if (hasProcessedNull)
                {
                    fefoQuery = fefoQuery.Where(b => b.ExpirationDate.HasValue);
                }
            }

            var fefoBatches = await fefoQuery
                .OrderBy(b => b.ExpirationDate.HasValue ? 0 : 1)  // null 批次排末尾
                .ThenBy(b => b.ExpirationDate)                    // FEFO: 近效期优先
                .ThenBy(b => b.PurchaseDate)                      // 同效期 FIFO
                .ThenBy(b => b.CreatedTime)                       // null 批次按 CreatedTime 升序，兜底稳定排序
                .ToListAsync();

            remaining = await DeductFromBatchesAsync(order, item, fefoBatches, remaining, now, sourceType, createSampleGiftOut);
        }

        // 3. 仍有剩余 = 库存不足
        if (remaining > 0)
        {
            if (dtoItem.ExpirationDates != null && dtoItem.ExpirationDates.Any() && !dtoItem.AllowAutoFillBeyondSelection)
            {
                // 选中效期不足且未允许自动补足 -> 构造 409 结构化错误
                // 注：包含 null 批次，作为"无"效期选项展示在末尾
                var availableBatches = await _dbContext.InventoryBatches
                    .Where(b => b.ProductId == dtoItem.ProductId
                        && b.Status == 1
                        && b.Quantity > 0
                        && b.TenantId == order.TenantId
                        && b.StoreId == order.StoreId)
                    .ToListAsync();

                // 分组：有值按 Date 分组，null 单独作为一组（"无"效期）
                var availableOptions = availableBatches
                    .GroupBy(b => b.ExpirationDate.HasValue ? (DateTime?)b.ExpirationDate.Value.Date : null)
                    .Select(g => new { ExpirationDate = g.Key, TotalQuantity = g.Sum(b => b.Quantity) })
                    .OrderBy(x => x.ExpirationDate.HasValue ? 0 : 1)  // null 批次排末尾
                    .ThenBy(x => x.ExpirationDate)                    // 近效期优先
                    .ToList();

                // 序列化：null 用 "无" 表示
                var optionsStr = string.Join(";",
                    availableOptions.Select(x =>
                        x.ExpirationDate.HasValue
                            ? $"{x.ExpirationDate:yyyy-MM-dd}:{x.TotalQuantity}"
                            : $"无:{x.TotalQuantity}"));

                throw new InvalidOperationException(
                    $"INSUFFICIENT_EXPIRY_STOCK|{dtoItem.ProductId}|{item.ProductName}|{remaining}|{optionsStr}");
            }

            throw new InvalidOperationException($"商品 {item.ProductName} 库存不足（还差 {remaining} 件）");
        }
    }

    /// <summary>
    /// 从指定批次列表中逐批扣减库存，更新 Inventory 汇总表，记 InventoryLog。
    /// 返回未满足的剩余数量（0 表示全部扣减完成）。
    /// 通过 sourceType 区分销售出库（SalesOutbound）与赠品出库（GiftOutbound），
    /// 通过 createSampleGiftOut 控制是否额外创建 SampleGiftOut 记录（赠品出库路径使用）。
    /// </summary>
    private async Task<decimal> DeductFromBatchesAsync(
        OrderEntity order, OrderItem item, List<InventoryBatch> batches, decimal needQty, DateTime now,
        int sourceType, bool createSampleGiftOut)
    {
        var remaining = needQty;

        foreach (var batch in batches)
        {
            if (remaining <= 0) break;

            var deduct = Math.Min(batch.Quantity, remaining);
            var beforeQty = batch.Quantity;

            // 扣减批次
            batch.Quantity -= deduct;
            batch.UpdatedTime = now;
            if (batch.Quantity <= 0)
            {
                batch.Status = 2; // 已用完
            }

            // 更新库存汇总
            var inventory = await _dbContext.Inventories
                .FirstOrDefaultAsync(inv => inv.ProductId == item.ProductId
                    && inv.TenantId == order.TenantId
                    && inv.StoreId == order.StoreId);
            if (inventory != null)
            {
                inventory.Quantity -= deduct;
                inventory.UpdatedTime = now;
            }

            // 记录库存流水（Type=2 出库），SourceType 由调用方传入区分销售/赠品出库
            // P-SG-02：赠品出库统一使用 GiftOutbound，与 SampleGiftOutAppService.CreateAsync 一致
            var remark = sourceType == InventoryLogSourceTypes.GiftOutbound
                ? $"赠品出库-{order.OrderNo}"
                : $"零售出库-{order.OrderNo}";
            _dbContext.InventoryLogs.Add(new InventoryLog
            {
                ProductId = item.ProductId,
                Type = 2,
                SourceType = sourceType,
                UnitPrice = batch.UnitPrice,
                Quantity = -deduct,
                BeforeQuantity = beforeQty,
                AfterQuantity = batch.Quantity,
                BatchNo = batch.BatchNo,
                ExpirationDate = batch.ExpirationDate,
                RelatedId = order.Id,
                Remark = remark,
                TenantId = order.TenantId,
                TenantCode = order.TenantCode,
                StoreId = order.StoreId,
                StoreCode = order.StoreCode,
                CreatedTime = now
            });

            // 记录订单明细批次扣减（OrderItemBatch：效期追溯和统计的数据源）
            // 赠品与零售商品统一写入，保证效期追溯能力一致
            _dbContext.OrderItemBatches.Add(new OrderItemBatch
            {
                OrderItemId = item.Id,
                OrderId = order.Id,
                ProductId = item.ProductId,
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

            // 赠品出库路径：每个扣减批次创建独立 SampleGiftOut 记录
            // 与 SampleGiftOut 实体的"单批次"设计一致；跨批次扣减时创建多条记录，均关联同一 OrderId
            if (createSampleGiftOut)
            {
                _dbContext.SampleGiftOuts.Add(new SampleGiftOut
                {
                    ProductId = item.ProductId,
                    InventoryBatchId = batch.Id,
                    Quantity = deduct,
                    OutTime = now,
                    OrderId = order.Id,
                    OperatorId = order.OperatorId,
                    Remark = $"订单赠品-{order.OrderNo}",
                    TenantId = order.TenantId,
                    TenantCode = order.TenantCode,
                    StoreId = order.StoreId,
                    StoreCode = order.StoreCode,
                    CreatedTime = now
                });
            }

            remaining -= deduct;
        }

        return remaining;
    }

    /// <summary>
    /// 服务耗材出库：按BOM计算耗材需求，FEFO（最近效期优先）自动选择批次扣减
    /// 扣减明细记录到 OrderItemBatch（替代原 ConsumableDeduction JSON），支持效期追溯和成本归集
    /// </summary>
    private async Task DeductServiceBomAsync(OrderEntity order, DateTime now)
    {
        foreach (var item in order.OrderItems)
        {
            // 查找服务项目的BOM
            var boms = await _dbContext.ServiceBoms
                .Where(b => b.ServiceProductId == item.ProductId && b.TenantId == order.TenantId)
                .ToListAsync();

            if (!boms.Any()) continue;

            decimal totalConsumableCost = 0m;

            foreach (var bom in boms)
            {
                // 需要扣减的数量 = BOM单次消耗量 × 订单数量
                var needQty = bom.Quantity * item.Quantity;

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

                    // 更新汇总
                    var inventory = await _dbContext.Inventories
                        .FirstOrDefaultAsync(inv => inv.ProductId == bom.ConsumableProductId && inv.TenantId == order.TenantId && inv.StoreId == order.StoreId);
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
                        SourceType = InventoryLogSourceTypes.SalesOutbound,
                        UnitPrice = batch.UnitPrice,
                        Quantity = -deduct,
                        BeforeQuantity = beforeQty,
                        AfterQuantity = batch.Quantity,
                        BatchNo = batch.BatchNo,
                        ExpirationDate = batch.ExpirationDate,
                        RelatedId = order.Id,
                        Remark = $"服务耗材出库-{order.OrderNo}",
                        TenantId = order.TenantId,
                        TenantCode = order.TenantCode,
                        StoreId = order.StoreId,
                        StoreCode = order.StoreCode,
                        CreatedTime = now
                    });

                    // 记录订单明细批次扣减（OrderItemBatch：效期追溯和统计的数据源）
                    // ProductId 记录耗材ID（bom.ConsumableProductId），便于按耗材统计效期消耗
                    _dbContext.OrderItemBatches.Add(new OrderItemBatch
                    {
                        OrderItemId = item.Id,
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

                    totalConsumableCost += deduct * batch.UnitPrice;
                    remaining -= deduct;
                }

                if (remaining > 0)
                    throw new InvalidOperationException($"耗材(ID:{bom.ConsumableProductId})库存不足，还需 {remaining}");
            }

            // 归集耗材成本到服务项目（OrderItemBatch 已结构化存储明细，无需 JSON 序列化）
            if (totalConsumableCost > 0m)
            {
                item.ConsumableCost = totalConsumableCost;
            }
        }
    }

    /// <summary>
    /// 储值扣减：委托 IStoredValueAccountAppService.ConsumeAsync 在当前事务内完成
    /// 先扣实收余额，再扣赠送余额，记录 StoredValueLog
    /// </summary>
    /// <param name="amount">扣减金额（单一储值支付时为 order.PaidAmount）</param>
    private async Task DeductStoredValueAsync(OrderEntity order, decimal amount, DateTime now)
    {
        if (!order.CustomerId.HasValue)
            throw new InvalidOperationException("储值支付订单必须指定客户");
        if (amount <= 0)
            throw new InvalidOperationException("储值扣减金额必须大于0");

        var result = await _storedValueAccountAppService.ConsumeAsync(
            order.CustomerId.Value, order.TenantId, order.TenantCode,
            order.StoreId, order.StoreCode,
            amount, order.Id, order.OrderNo,
            5, // 单一储值支付
            now);

        if (!result.Success)
            throw new InvalidOperationException(result.ErrorMessage);
    }

    /// <summary>
    /// 积分抵扣：按 PointsRule.DeductRate 计算扣减积分，校验充足后扣减客户积分并记录兑换
    /// </summary>
    /// <param name="amount">抵扣金额（单一积分支付时为 order.PaidAmount；组合支付时为 PointsAmount）</param>
    private async Task DeductPointsAsync(OrderEntity order, decimal amount, DateTime now)
    {
        if (!order.CustomerId.HasValue)
            throw new InvalidOperationException("积分抵扣订单必须指定客户");
        if (amount <= 0)
            throw new InvalidOperationException("积分抵扣金额必须大于0");

        var rule = await GetEffectivePointsRuleAsync(order.TenantId, order.StoreId);
        if (rule == null || rule.DeductRate <= 0)
            throw new InvalidOperationException("未配置启用的积分规则或抵扣比例为0");

        // 校验抵扣金额不超过单笔上限
        if (rule.MaxDeductAmount > 0 && amount > rule.MaxDeductAmount)
            throw new InvalidOperationException($"积分抵扣金额超过单笔上限（{rule.MaxDeductAmount:F2}）");

        // 计算需扣减的积分（向上取整，避免少扣）
        var pointsToDeduct = (int)Math.Ceiling(amount / rule.DeductRate);

        var customer = await _dbContext.Customers
            .FirstOrDefaultAsync(c => c.Id == order.CustomerId.Value && c.TenantId == order.TenantId);
        if (customer == null)
            throw new InvalidOperationException("客户不存在");

        if (customer.TotalPoints < pointsToDeduct)
            throw new InvalidOperationException($"客户积分不足（当前 {customer.TotalPoints}，需要 {pointsToDeduct}）");

        customer.TotalPoints -= pointsToDeduct;
        customer.UpdatedTime = now;

        // 仅单一积分支付（PayMethod=6）在此更新累计消费；组合支付（PayMethod=7）由 AwardPointsAsync 统一处理 CashAmount+StoredValueAmount
        if (order.PayMethod == 6)
        {
            customer.TotalConsume += amount;
            customer.LastConsumeTime = now;
        }

        _dbContext.PointsExchanges.Add(new PointsExchange
        {
            CustomerId = customer.Id,
            ExchangeType = 3, // 服务项目积分消费
            TargetId = null,
            TargetName = $"订单积分抵扣-{order.OrderNo}",
            PointsCost = pointsToDeduct,
            Quantity = 1,
            ExchangeTime = now,
            OperatorId = order.OperatorId,
            Remark = $"订单 {order.OrderNo} 积分抵扣 {amount:F2}元",
            TenantId = order.TenantId,
            TenantCode = order.TenantCode,
            CreatedTime = now
        });
    }

    /// <summary>
    /// 查询生效的积分规则：优先门店级（StoreId 匹配），回退租户级默认规则（StoreId=0）
    /// 修复原查询未按 StoreId 过滤的 bug，支持门店级配置回退租户级
    /// </summary>
    private async Task<PointsRule?> GetEffectivePointsRuleAsync(long tenantId, long storeId)
    {
        // 优先查门店级规则
        var rule = await _dbContext.PointsRules
            .FirstOrDefaultAsync(r => r.TenantId == tenantId && r.StoreId == storeId && r.Status == 1);
        if (rule != null) return rule;

        // 回退到租户级默认规则（StoreId=0 表示租户级通用规则）
        return await _dbContext.PointsRules
            .FirstOrDefaultAsync(r => r.TenantId == tenantId && r.StoreId == 0 && r.Status == 1);
    }

    /// <summary>
    /// 积分发放：按积分规则 PointsRate 计算，Floor 取整
    /// 内置显式防护：OrderType=3 疗程卡核销直接返回（购买时已发放）
    /// PayMethod=5/6 不发积分的判断已由 CreateAsync 调用方控制，本方法不再重复
    /// </summary>
    /// <param name="baseAmount">发放基数（单一支付为 PaidAmount；组合支付为 CashAmount+StoredValueAmount，排除积分抵扣部分）</param>
    private async Task AwardPointsAsync(OrderEntity order, decimal baseAmount, DateTime now)
    {
        if (!order.CustomerId.HasValue || baseAmount <= 0) return;

        // 显式防护：疗程卡核销不发积分（购买时已发放），与 CreateAsync 调用处条件形成双重保险
        if (order.OrderType == 3) return;

        var pointsRule = await GetEffectivePointsRuleAsync(order.TenantId, order.StoreId);
        if (pointsRule == null || pointsRule.PointsRate <= 0) return;

        var points = (int)Math.Floor(baseAmount * pointsRule.PointsRate);
        if (points <= 0) return;

        order.Points = points;

        var customer = await _dbContext.Customers
            .FirstOrDefaultAsync(c => c.Id == order.CustomerId.Value && c.TenantId == order.TenantId);
        if (customer == null) return;

        var beforePoints = customer.TotalPoints;
        customer.TotalPoints += points;
        // 累计消费按发积分基数计入（组合支付排除积分抵扣部分，避免重复计算）
        customer.TotalConsume += baseAmount;
        customer.LastConsumeTime = now;
        customer.UpdatedTime = now;

        var expireDate = pointsRule.PointsValidityDays.HasValue
            ? now.AddDays(pointsRule.PointsValidityDays.Value)
            : (DateTime?)null;

        _dbContext.CustomerPointsLogs.Add(new CustomerPointsLog
        {
            CustomerId = customer.Id,
            Type = CustomerPointsLogType.Consume, // 消费获得
            Points = points,
            BeforePoints = beforePoints,
            AfterPoints = customer.TotalPoints,
            OrderId = order.Id,
            OperatorId = _currentUser.UserId,
            ExpireDate = expireDate,
            Remark = $"订单 {order.OrderNo} 消费获得积分",
            TenantId = order.TenantId,
            TenantCode = order.TenantCode,
            StoreId = order.StoreId,
            StoreCode = order.StoreCode,
            CreatedTime = now
        });
    }

    /// <summary>
    /// 组合支付扣减（PayMethod=7）：
    /// - 类别1：仅记录 CashAmount/CashPayMethod（线下或第三方收款，系统不联动扣减）
    /// - 类别2：调用 IStoredValueAccountAppService.ConsumeAsync 扣减储值余额（先实收后赠送）
    /// - 类别3：调用 DeductPointsAsync 扣减积分（按门店级 PointsRule 配置）
    /// 三类扣减在调用方事务内完成，任一失败抛异常回滚
    /// 优先级：类别1 > 类别2 > 类别3（前端联动计算保证，后端按字段独立扣减）
    /// </summary>
    private async Task DeductCombinedPaymentAsync(OrderEntity order, DateTime now)
    {
        var svAmount = order.StoredValueAmount ?? 0m;
        var pointsAmount = order.PointsAmount ?? 0m;

        // 类别2：储值扣减（委托 ConsumeAsync 在当前事务内完成）
        if (svAmount > 0)
        {
            if (!order.CustomerId.HasValue)
                throw new InvalidOperationException("组合支付使用储值扣减时必须指定客户");

            var result = await _storedValueAccountAppService.ConsumeAsync(
                order.CustomerId.Value, order.TenantId, order.TenantCode,
                order.StoreId, order.StoreCode,
                svAmount, order.Id, order.OrderNo,
                7, // 组合支付
                now);

            if (!result.Success)
                throw new InvalidOperationException(result.ErrorMessage);
        }

        // 类别3：积分抵扣（DeductPointsAsync 内部按 order.PayMethod=7 跳过累计消费更新，由 AwardPointsAsync 统一处理）
        if (pointsAmount > 0)
        {
            await DeductPointsAsync(order, pointsAmount, now);
        }
    }

    /// <summary>
    /// 记录消费记录 + 更新客户累计消费（积分由 AwardPointsAsync 处理，此处仅处理非积分订单的客户更新）
    /// 组合支付（PayMethod=7）时累计消费 = CashAmount + StoredValueAmount（积分抵扣部分不计入）
    /// </summary>
    private async Task RecordConsumeLogAsync(OrderEntity order, DateTime now)
    {
        if (!order.CustomerId.HasValue) return;

        _dbContext.ConsumeLogs.Add(new ConsumeLog
        {
            CustomerId = order.CustomerId.Value,
            OrderId = order.Id,
            Amount = order.PaidAmount,
            Points = order.Points,
            ConsumeTime = now,
            Remark = $"订单 {order.OrderNo}",
            TenantId = order.TenantId,
            TenantCode = order.TenantCode,
            CreatedTime = now
        });

        // 更新客户累计消费和最后消费时间（积分已在 AwardPointsAsync 中处理）
        var customer = await _dbContext.Customers
            .FirstOrDefaultAsync(c => c.Id == order.CustomerId.Value && c.TenantId == order.TenantId);
        if (customer != null)
        {
            // 如果 AwardPointsAsync 未更新（如 PayMethod=5/6 或 OrderType=3），此处补充更新
            // PayMethod=7 && OrderType!=3 时 AwardPointsAsync 已更新（按 CashAmount+StoredValueAmount），此处不补充
            // PayMethod=7 && OrderType==3 时 AwardPointsAsync 不调用，此处补充累计消费 = CashAmount + StoredValueAmount
            if (order.PayMethod == 5 || order.PayMethod == 6 || order.OrderType == 3)
            {
                var consumeAmount = order.PayMethod == 7
                    ? (order.CashAmount ?? 0m) + (order.StoredValueAmount ?? 0m)
                    : order.PaidAmount;
                customer.TotalConsume += consumeAmount;
                customer.LastConsumeTime = now;
                customer.UpdatedTime = now;
            }
        }
    }

    /// <summary>
    /// 记录商品销售统计（同日同商品累加 SalesCount/SalesAmount）
    /// ProductType 由 OrderType 映射：1零售->1, 2服务->2, 3疗程卡核销->4
    /// 赠品（Type=5）不计入销售统计，由 giftProductIds 跳过（V4 方案，P-SG-03）
    /// </summary>
    private async Task RecordProductSalesStatAsync(OrderEntity order, HashSet<long> giftProductIds, DateTime now)
    {
        // OrderType 到 ProductType 的映射
        var productType = order.OrderType switch
        {
            1 => 1, // 零售
            2 => 2, // 服务
            3 => 4, // 疗程卡核销
            _ => 0
        };
        if (productType == 0) return;

        var statDate = order.OrderTime.Date;
        var statMonth = $"{statDate:yyyy-MM}";

        foreach (var item in order.OrderItems)
        {
            // 赠品（Type=5）不计入销售统计：SalesCount/SalesAmount 仅统计可销售商品
            if (giftProductIds.Contains(item.ProductId)) continue;

            // 查找当天同商品的统计记录
            var existing = await _dbContext.ProductSalesStats
                .FirstOrDefaultAsync(s => s.TenantId == order.TenantId
                    && s.StoreId == order.StoreId
                    && s.ProductId == item.ProductId
                    && s.ProductType == productType
                    && s.StatDate == statDate);

            if (existing != null)
            {
                // 累加
                existing.SalesCount += (int)Math.Round(item.Quantity);
                existing.SalesAmount += item.DiscountedAmount;
                existing.UpdatedTime = now;
            }
            else
            {
                // 新建
                _dbContext.ProductSalesStats.Add(new ProductSalesStat
                {
                    StatDate = statDate,
                    StatMonth = statMonth,
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    ProductType = productType,
                    SalesCount = (int)Math.Round(item.Quantity),
                    SalesAmount = item.DiscountedAmount,
                    TenantId = order.TenantId,
                    TenantCode = order.TenantCode,
                    StoreId = order.StoreId,
                    StoreCode = order.StoreCode,
                    CreatedTime = now
                });
            }
        }
    }

    /// <summary>
    /// 退款时扣减商品销售统计（按退款比例扣减 SalesCount/SalesAmount）
    /// 按订单下单日期查找对应统计记录
    /// </summary>
    private async Task RefundProductSalesStatAsync(OrderEntity order, decimal ratio, List<string> actions, DateTime now)
    {
        var productType = order.OrderType switch
        {
            1 => 1,
            2 => 2,
            3 => 4,
            _ => 0
        };
        if (productType == 0) return;

        var statDate = order.OrderTime.Date;
        var items = await _dbContext.OrderItems
            .Where(oi => oi.OrderId == order.Id)
            .ToListAsync();

        foreach (var item in items)
        {
            var stat = await _dbContext.ProductSalesStats
                .FirstOrDefaultAsync(s => s.TenantId == order.TenantId
                    && s.StoreId == order.StoreId
                    && s.ProductId == item.ProductId
                    && s.ProductType == productType
                    && s.StatDate == statDate);

            if (stat == null) continue;

            var deductCount = (int)Math.Round(item.Quantity * ratio);
            var deductAmount = Math.Round(item.DiscountedAmount * ratio, 2);

            stat.SalesCount = Math.Max(0, stat.SalesCount - deductCount);
            stat.SalesAmount = Math.Max(0, stat.SalesAmount - deductAmount);
            stat.UpdatedTime = now;
        }

        actions.Add($"扣减商品销售统计（退款比例 {ratio:P1}）");
    }

    /// <summary>
    /// 取消订单（事务包裹，按 OrderType 全量回滚库存/BOM/疗程卡/积分/储值/统计/消费记录）
    /// 订单 Status 改为 4（已取消），视为订单未发生
    /// 仅 Status=1(进行中) 或 Status=2(已完成) 的订单可取消；已退款(3)或已取消(4)的订单不可取消
    /// 取消原因记录在 RefundReason 字段（语义为"订单回滚原因"，由 Status 区分取消/退款）
    /// </summary>
    public async Task<ApiResponseDto> CancelAsync(long id, OrderCancelDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("无法确定当前租户", 401);

        var validation = await _cancelValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var actions = new List<string>();
        var now = DateTime.Now;

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            var order = await _dbContext.Orders
                .FirstOrDefaultAsync(o => o.Id == id && o.TenantId == tenantId);
            if (order == null)
                return ApiResponseDto.Fail("订单不存在", 404);

            // 仅进行中(1)或已完成(2)的订单可取消；已退款(3)或已取消(4)的订单不可取消
            if (order.Status != 1 && order.Status != 2)
                return ApiResponseDto.Fail("仅进行中或已完成的订单可取消", 400);

            // 更新订单状态为已取消，记录取消原因（复用 RefundReason 字段存储 reversal reason，由 Status 区分取消/退款）
            order.Status = 4;
            order.RefundTime = now;
            order.RefundReason = dto.Reason;
            order.UpdatedTime = now;

            // 全量回滚库存/BOM/疗程卡/积分/储值/统计/消费记录
            await ReverseOrderAsync(order, actions, now);

            // 跨日取消订单触发原下单日反日结（P-DS-03 边界场景）
            // 若订单下单日早于今日，对该日的 DailySettlement 反日结或补建，提示店主重新确认
            var reverseReason = $"跨日取消订单自动反日结：订单 {order.OrderNo}（实付 {order.PaidAmount:F2} 元）";
            var reversedDates = await ReverseSettlementForOrderReversalAsync(order, reverseReason, now);
            if (reversedDates.Any())
            {
                actions.Add($"订单下单日 {string.Join("、", reversedDates.Select(d => d.ToString("yyyy-MM-dd")))} 日结已自动反日结/补建，请前往日结管理重新确认");
            }

            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            var message = reversedDates.Any()
                ? $"取消成功，订单下单日 {string.Join("、", reversedDates.Select(d => d.ToString("yyyy-MM-dd")))} 日结已自动反日结/补建，请前往日结管理重新确认"
                : "取消成功";
            return ApiResponseDto.Success(null, message);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// 订单退款（事务包裹，按 OrderType 联动库存/疗程卡/储值/积分）
    /// 支持部分退款：按 refundAmount / paidAmount 比例计算退库存和扣积分
    /// 全额退款时订单状态改为 3(已退款)
    /// OrderType=3 疗程卡核销订单：PaidAmount=0，RefundAmount 必须为0，按全额回滚疗程卡次数与库存/BOM
    /// </summary>
    public async Task<ApiResponseDto<RefundResultDto>> RefundAsync(RefundRequestDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<RefundResultDto>.Fail("无法确定当前租户", 401);

        var validation = await _refundValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<RefundResultDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var actions = new List<string>();
        var now = DateTime.Now;

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            // 1. 加载订单
            var order = await _dbContext.Orders
                .FirstOrDefaultAsync(o => o.Id == dto.OrderId && o.TenantId == tenantId);
            if (order == null)
                return ApiResponseDto<RefundResultDto>.Fail("订单不存在", 404);

            // 2. 校验订单状态（仅已完成可退款）
            if (order.Status != 2)
                return ApiResponseDto<RefundResultDto>.Fail("仅已完成的订单可退款", 400);

            // 3. 按 OrderType 校验退款金额
            // OrderType=3 核销订单 PaidAmount=0，RefundAmount 必须为0（仅回退疗程卡次数与库存/BOM，无款项退还）
            // OrderType=1/2 必须有实际退款金额（RefundAmount > 0）
            if (order.OrderType == 3 && dto.RefundAmount != 0)
                return ApiResponseDto<RefundResultDto>.Fail("疗程卡核销订单无实际款项，退款金额必须为0", 400);
            if (order.OrderType != 3 && dto.RefundAmount <= 0)
                return ApiResponseDto<RefundResultDto>.Fail("退款金额必须大于0", 400);

            // 3.1 校验退款金额（不超过实付金额减去已退款金额，OrderType=3 跳过：PaidAmount=0）
            if (order.OrderType != 3)
            {
                var remainingRefundable = order.PaidAmount - order.RefundAmount;
                if (dto.RefundAmount > remainingRefundable)
                    return ApiResponseDto<RefundResultDto>.Fail($"退款金额超出可退金额（最多可退 {remainingRefundable:F2} 元）", 400);
            }

            // 退款比例（用于按比例退库存和扣积分）
            // OrderType=3 PaidAmount=0 但需全额回滚库存/BOM，强制 ratio=1.0
            var ratio = order.OrderType == 3
                ? 1.0m
                : (order.PaidAmount > 0 ? dto.RefundAmount / order.PaidAmount : 0m);

            // 4. 更新订单退款信息（OrderType=3 RefundAmount=0，仅更新退款时间和原因）
            order.RefundAmount += dto.RefundAmount;
            order.RefundTime = now;
            order.RefundReason = dto.Reason;
            order.UpdatedTime = now;
            // OrderType=3 退款即全额回滚，直接置为已退款；其他类型按累计退款金额判断
            if (order.OrderType == 3 || order.RefundAmount >= order.PaidAmount)
            {
                order.Status = 3; // 已退款
            }

            // 5. 按 OrderType 联动
            switch (order.OrderType)
            {
                case 1: // 零售：按比例退库存
                    await RefundRetailInventoryAsync(order, ratio, actions, now);
                    break;
                case 2: // 服务：按比例退 BOM 耗材库存（创建时已写入 OrderItemBatch，统一走批次退库）
                    await RefundRetailInventoryAsync(order, ratio, actions, now);
                    break;
                case 3: // 疗程卡核销：回退次数 + 全额退商品/BOM 耗材库存
                    await RefundTreatmentCardVerifyAsync(order, actions, now);
                    await RefundRetailInventoryAsync(order, ratio, actions, now);
                    break;
            }

            // 6. 通用：积分扣减（按比例），积分不足时差额折算现金从退款扣除
            decimal pointsCashDeduction = 0;
            if (order.Points > 0)
            {
                pointsCashDeduction = await RefundPointsAsync(order, ratio, actions, now);
                if (pointsCashDeduction > 0)
                {
                    order.RefundAmount -= pointsCashDeduction;
                    if (order.RefundAmount >= order.PaidAmount)
                    {
                        order.Status = 3; // 已退款
                    }
                }
            }

            // 7. 按 PayMethod 联动：储值支付退款（退到实收余额，赠送余额不退，用扣除折算后的实际退款金额）
            var actualRefundAmount = dto.RefundAmount - pointsCashDeduction;
            if (order.PayMethod == 5 && actualRefundAmount > 0)
            {
                await RefundStoredValueAsync(order, actualRefundAmount, actions, now);
            }
            // 7.1 按 PayMethod 联动：积分抵扣退款（按 DeductRate 退还积分到客户账户）
            if (order.PayMethod == 6 && actualRefundAmount > 0)
            {
                await RefundPointsPaymentAsync(order, actualRefundAmount, actions, now);
            }
            // 7.2 按 PayMethod 联动：组合支付退款（按 CashAmount/StoredValueAmount/PointsAmount 比例分摊退款金额）
            if (order.PayMethod == 7 && actualRefundAmount > 0)
            {
                await RefundCombinedPaymentAsync(order, actualRefundAmount, actions, now);
            }

            // 8. 扣减商品销售统计（ProductSalesStat，按退款比例）
            await RefundProductSalesStatAsync(order, ratio, actions, now);

            // 9. 跨日退款触发原下单日反日结（P-DS-03 边界场景）
            // 若订单下单日早于今日，对该日的 DailySettlement 反日结或补建，提示店主重新确认
            var reverseReason = $"跨日退款自动反日结：订单 {order.OrderNo} 退款 {actualRefundAmount:F2} 元";
            var reversedDates = await ReverseSettlementForOrderReversalAsync(order, reverseReason, now);
            if (reversedDates.Any())
            {
                actions.Add($"订单下单日 {string.Join("、", reversedDates.Select(d => d.ToString("yyyy-MM-dd")))} 日结已自动反日结/补建，请前往日结管理重新确认");
            }

            // 10. 全额退款时删除订单关联的赠品出库记录（部分退款保留出库记录以保留部分出库事实）
            if (order.Status == 3)
            {
                await ReverseSampleGiftOutAsync(order, actions);
            }

            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            var message = reversedDates.Any()
                ? $"退款成功，订单下单日 {string.Join("、", reversedDates.Select(d => d.ToString("yyyy-MM-dd")))} 日结已自动反日结，请前往日结管理重新确认"
                : "退款成功";

            return ApiResponseDto<RefundResultDto>.Ok(new RefundResultDto
            {
                OrderId = order.Id,
                OrderNo = order.OrderNo,
                ThisRefundAmount = actualRefundAmount,
                TotalRefundedAmount = order.RefundAmount,
                OrderStatus = order.Status,
                RefundTime = now,
                Actions = actions,
                ReversedSettlementDates = reversedDates
            }, message);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// 跨日订单回滚（退款/取消）触发的原下单日反日结或补建（P-DS-03 边界场景修复）
    /// 若订单下单日早于今日，对该日的 DailySettlement 执行：
    /// - 已确认(Status=1)：状态置 0，记录反日结原因，重新汇总字段（DailyStat 等店主重新确认时由 ConfirmAsync 同步）
    /// - 待确认(Status=0)：仅重新汇总字段（数据已是最新）
    /// - 不存在：创建一条待确认记录（兜底任务本应覆盖 30 天内日期，未覆盖视为 bug，需补建等店主确认）
    /// 多次退款同一日：仅首次从已确认 -> 待确认时记录反日结原因，后续只更新字段
    /// </summary>
    /// <param name="order">回滚订单实体（已更新 RefundAmount/Status 等字段）</param>
    /// <param name="reason">反日结/补建原因（写入 ReversedReason 或 Remark，由调用方组织文案）</param>
    /// <param name="now">操作时间</param>
    /// <returns>本次触发反日结/补建的日期列表</returns>
    private async Task<List<DateTime>> ReverseSettlementForOrderReversalAsync(
        OrderEntity order, string reason, DateTime now)
    {
        var reversedDates = new List<DateTime>();
        var orderDate = order.OrderTime.Date;
        // 当日（或未来日，理论上不应出现）订单无需反日结
        if (orderDate >= DateTime.Today)
            return reversedDates;

        var settlement = await _dbContext.DailySettlements
            .FirstOrDefaultAsync(s => s.TenantId == order.TenantId
                && s.StoreId == order.StoreId
                && s.SettlementDate == orderDate);

        // 重新汇总该日数据（无论原状态，都更新字段，确保数据最新）
        var data = await _dailySettlementAppService.SummarizeCoreAsync(
            order.TenantId, order.StoreId, orderDate);

        if (settlement == null)
        {
            // 原下单日无日结记录：兜底任务本应覆盖 30 天内日期，未覆盖视为 bug
            // 创建一条待确认记录，等店主审核确认
            var storeCode = await _dbContext.Stores
                .Where(s => s.TenantId == order.TenantId && s.Id == order.StoreId)
                .Select(s => s.Code)
                .FirstOrDefaultAsync() ?? string.Empty;

            settlement = new DailySettlement
            {
                TenantId = order.TenantId,
                TenantCode = _currentUser.TenantCode ?? string.Empty,
                StoreId = order.StoreId,
                StoreCode = storeCode,
                SettlementDate = orderDate,
                Status = 0,    // 待确认
                Source = 1,    // 视为手动触发（区别于兜底任务 Source=2）
                Remark = reason,
                CreatedTime = now
            };
            ApplySummaryToSettlement(settlement, data, _currentUser.UserId, now);
            _dbContext.DailySettlements.Add(settlement);
        }
        else
        {
            // 已确认状态执行反日结（待确认状态仅更新数据）
            if (settlement.Status == 1)
            {
                settlement.Status = 0;
                settlement.ReversedBy = _currentUser.UserId;
                settlement.ReversedTime = now;
                settlement.ReversedReason = reason;
                settlement.ConfirmedBy = null;
                settlement.ConfirmedTime = null;
            }
            ApplySummaryToSettlement(settlement, data, _currentUser.UserId, now);
        }

        reversedDates.Add(orderDate);
        return reversedDates;
    }

    /// <summary>
    /// 将汇总数据应用到 DailySettlement 实体字段
    /// </summary>
    private static void ApplySummaryToSettlement(
        DailySettlement settlement, SettlementSummaryData data, long? operatorId, DateTime now)
    {
        settlement.TotalRevenue = data.TotalRevenue;
        settlement.CashRevenue = data.CashRevenue;
        settlement.StoredValueRevenue = data.StoredValueRevenue;
        settlement.PointsDeductAmount = data.PointsDeductAmount;
        settlement.TotalRefund = data.TotalRefund;
        settlement.CashRefundAmount = data.CashRefundAmount;
        settlement.TreatmentCardVerifyAmount = data.TreatmentCardVerifyAmount;
        settlement.TotalStoredValueRecharge = data.TotalStoredValueRecharge;
        settlement.TotalStoredValueConsume = data.TotalStoredValueConsume;
        settlement.OrderCount = data.OrderCount;
        settlement.TotalCost = data.TotalCost;
        settlement.SalesOutboundCost = data.SalesOutboundCost;
        settlement.TreatmentCardOutboundCost = data.TreatmentCardOutboundCost;
        settlement.InventoryLossAmount = data.InventoryLossAmount;
        settlement.SampleGiftAmount = data.SampleGiftAmount;
        settlement.TransferOutAmount = data.TransferOutAmount;
        settlement.TransferInAmount = data.TransferInAmount;
        settlement.PurchaseReturnAmount = data.PurchaseReturnAmount;
        settlement.TotalGrossProfit = data.TotalGrossProfit;
        settlement.SettlementTime = now;
        settlement.OperatorId = operatorId;
        settlement.UpdatedTime = now;
    }

    /// <summary>
    /// 订单全量回滚内部方法（取消订单使用）：按 OrderType 联动回滚库存/BOM/疗程卡/积分/储值/统计/消费记录
    /// 调用方需已开启事务并加载订单实体。本方法不更新订单状态（由调用方设置 Status=4 已取消）
    /// 与 RefundAsync 的区别：
    /// - 全量回滚（ratio=1.0），不支持部分金额
    /// - 删除 ConsumeLog（取消订单视为未发生消费）
    /// - 回退 Customer.TotalConsume（按订单实付金额）
    /// 注意：Customer.LastConsumeTime 不回退（无法准确还原历史值，且为非关键展示字段）
    /// </summary>
    private async Task ReverseOrderAsync(OrderEntity order, List<string> actions, DateTime now)
    {
        const decimal ratio = 1.0m;

        // 1. 按 OrderType 联动库存/BOM/疗程卡
        // OrderType=1 零售、OrderType=2 服务（BOM 耗材）、OrderType=3 核销（商品/BOM 耗材）
        // 创建时均通过 OrderItemBatch 记录批次扣减，统一调用 RefundRetailInventoryAsync 按批次精确退库
        switch (order.OrderType)
        {
            case 1: // 零售：退实物商品库存
                await RefundRetailInventoryAsync(order, ratio, actions, now);
                break;
            case 2: // 服务：退 BOM 耗材库存（创建时已写入 OrderItemBatch）
                await RefundRetailInventoryAsync(order, ratio, actions, now);
                break;
            case 3: // 疗程卡核销：回退次数 + 退商品/BOM 耗材库存
                await RefundTreatmentCardVerifyAsync(order, actions, now);
                await RefundRetailInventoryAsync(order, ratio, actions, now);
                break;
        }

        // 1.1 删除订单关联的赠品出库记录（取消订单视为未发生赠品出库，与批次库存回滚配套）
        await ReverseSampleGiftOutAsync(order, actions);

        // 2. 积分扣减（按全额比例，OrderType=3 核销不发积分，order.Points=0 自动跳过）
        if (order.Points > 0)
        {
            await RefundPointsAsync(order, ratio, actions, now);
        }

        // 3. 按 PayMethod 联动支付退款（全额 PaidAmount）
        // OrderType=3 核销订单 PaidAmount=0，以下条件自动跳过
        if (order.PayMethod == 5 && order.PaidAmount > 0)
        {
            await RefundStoredValueAsync(order, order.PaidAmount, actions, now);
        }
        if (order.PayMethod == 6 && order.PaidAmount > 0)
        {
            await RefundPointsPaymentAsync(order, order.PaidAmount, actions, now);
        }
        if (order.PayMethod == 7 && order.PaidAmount > 0)
        {
            await RefundCombinedPaymentAsync(order, order.PaidAmount, actions, now);
        }

        // 4. 扣减商品销售统计（全额）
        await RefundProductSalesStatAsync(order, ratio, actions, now);

        // 5. 删除消费记录（取消订单视为未发生消费）
        var consumeLogs = await _dbContext.ConsumeLogs
            .Where(cl => cl.OrderId == order.Id)
            .ToListAsync();
        if (consumeLogs.Any())
        {
            _dbContext.ConsumeLogs.RemoveRange(consumeLogs);
            actions.Add($"删除消费记录 {consumeLogs.Count} 条");
        }

        // 6. 回退客户累计消费（按订单实际计入金额）
        if (order.CustomerId.HasValue)
        {
            var customer = await _dbContext.Customers
                .FirstOrDefaultAsync(c => c.Id == order.CustomerId.Value && c.TenantId == order.TenantId);
            if (customer != null)
            {
                // 组合支付累计消费仅计 CashAmount + StoredValueAmount（积分抵扣部分不计入），与 RecordConsumeLogAsync 保持一致
                var consumeAmount = order.PayMethod == 7
                    ? (order.CashAmount ?? 0m) + (order.StoredValueAmount ?? 0m)
                    : order.PaidAmount;
                if (consumeAmount > 0)
                {
                    customer.TotalConsume = Math.Max(0m, customer.TotalConsume - consumeAmount);
                    customer.UpdatedTime = now;
                    actions.Add($"客户 ID:{customer.Id} 累计消费回退 {consumeAmount:F2}");
                }
            }
        }
    }

    /// <summary>
    /// 回滚订单关联的赠品出库记录（P-SG-03 配套修复）
    /// 取消订单或全额退款时，需删除该订单通过 DeductSampleGiftOutAsync 创建的 SampleGiftOut 记录，
    /// 与 RefundRetailInventoryAsync 退回的批次库存一一对应，避免出现"库存已退但出库记录仍存在"的数据不一致。
    /// 注意：SampleGiftOut 继承 StoreBusinessEntityBase，无软删除字段，采用物理删除。
    /// </summary>
    private async Task ReverseSampleGiftOutAsync(OrderEntity order, List<string> actions)
    {
        var sampleGiftOuts = await _dbContext.SampleGiftOuts
            .Where(sgo => sgo.OrderId == order.Id)
            .ToListAsync();
        if (!sampleGiftOuts.Any()) return;

        _dbContext.SampleGiftOuts.RemoveRange(sampleGiftOuts);
        actions.Add($"删除赠品出库记录 {sampleGiftOuts.Count} 条（订单 {order.OrderNo} 回滚）");
    }

    /// <summary>
    /// 零售订单退款：按 OrderItemBatch 精确退款到原批次，保留原效期信息。
    /// 若原批次仍 Status=1（在库）直接加回；若已用完或 BatchId=null 新建退货批次并写入原 ExpirationDate。
    /// 同步更新 OrderItemBatch.RefundedQuantity 以支持效期销售统计正确扣除退款部分。
    /// </summary>
    private async Task RefundRetailInventoryAsync(OrderEntity order, decimal ratio, List<string> actions, DateTime now)
    {
        // 查询订单所有 OrderItemBatch（按批次粒度精确退款，替代原按 OrderItem 退款逻辑）
        var itemBatches = await _dbContext.OrderItemBatches
            .Where(oib => oib.OrderId == order.Id && oib.RefundedQuantity < oib.Quantity)
            .ToListAsync();

        if (!itemBatches.Any())
        {
            // 兼容历史订单（无 OrderItemBatch 记录）：回退到原逻辑按 OrderItem 退款
            await RefundRetailInventoryLegacyAsync(order, ratio, actions, now);
            return;
        }

        // 预加载原批次信息（按 BatchId 分组查询，减少数据库往返）
        var batchIds = itemBatches.Where(x => x.BatchId.HasValue).Select(x => x.BatchId!.Value).Distinct().ToList();
        var originalBatches = await _dbContext.InventoryBatches
            .Where(b => batchIds.Contains(b.Id))
            .ToDictionaryAsync(b => b.Id);

        // 按 ProductId 分组更新 Inventory 汇总表（一次查询多个商品）
        var productIds = itemBatches.Select(x => x.ProductId).Distinct().ToList();
        var inventories = await _dbContext.Inventories
            .Where(i => productIds.Contains(i.ProductId) && i.TenantId == order.TenantId && i.StoreId == order.StoreId)
            .ToDictionaryAsync(i => i.ProductId);

        foreach (var itemBatch in itemBatches)
        {
            // 按比例计算本批次应退数量（受未退款数量约束）
            var remaining = itemBatch.Quantity - itemBatch.RefundedQuantity;
            var refundQty = Math.Round(remaining * ratio, 4);
            if (refundQty <= 0) continue;

            // 找原批次：优先 BatchId 关联，找不到则新建退货批次（保留原效期）
            InventoryBatch? targetBatch = null;
            if (itemBatch.BatchId.HasValue && originalBatches.TryGetValue(itemBatch.BatchId.Value, out var origBatch))
            {
                targetBatch = origBatch;
                targetBatch.Quantity += refundQty;
                targetBatch.UpdatedTime = now;
                // 若原批次已用完(Status=2)或已过期(Status=3)，恢复为在库
                if (targetBatch.Status != 1) targetBatch.Status = 1;
            }
            else
            {
                // 原批次不存在（已删除）或无 BatchId：新建退货批次，保留原 ExpirationDate 用于效期追溯
                targetBatch = new InventoryBatch
                {
                    ProductId = itemBatch.ProductId,
                    BatchNo = $"RET-{order.OrderNo}-{itemBatch.ProductId}-{itemBatch.Id}",
                    Quantity = refundQty,
                    UnitPrice = itemBatch.UnitPrice,
                    ExpirationDate = itemBatch.ExpirationDate,
                    Status = 1,
                    Remark = $"订单 {order.OrderNo} 退款退货（原批次 {itemBatch.BatchNo}）",
                    TenantId = order.TenantId,
                    TenantCode = order.TenantCode,
                    StoreId = order.StoreId,
                    StoreCode = order.StoreCode,
                    CreatedTime = now
                };
                _dbContext.InventoryBatches.Add(targetBatch);
            }

            // 更新 Inventory 汇总表
            var beforeQty = 0m;
            if (inventories.TryGetValue(itemBatch.ProductId, out var inventory))
            {
                beforeQty = inventory.Quantity;
                inventory.Quantity += refundQty;
                inventory.UpdatedTime = now;
            }
            else
            {
                // 无汇总记录时新建（极端情况）
                inventory = new Inventory
                {
                    ProductId = itemBatch.ProductId,
                    Quantity = refundQty,
                    AlertQuantity = 0,
                    TenantId = order.TenantId,
                    TenantCode = order.TenantCode,
                    StoreId = order.StoreId,
                    StoreCode = order.StoreCode,
                    CreatedTime = now
                };
                _dbContext.Inventories.Add(inventory);
                inventories[itemBatch.ProductId] = inventory;
            }

            // 记库存流水（Type=1入库, SourceType=退货入库，写入原 ExpirationDate 用于效期统计）
            _dbContext.InventoryLogs.Add(new InventoryLog
            {
                ProductId = itemBatch.ProductId,
                Type = 1,
                SourceType = InventoryLogSourceTypes.ReturnInbound,
                UnitPrice = itemBatch.UnitPrice,
                Quantity = refundQty,
                BeforeQuantity = beforeQty,
                AfterQuantity = beforeQty + refundQty,
                BatchNo = targetBatch.BatchNo,
                ExpirationDate = itemBatch.ExpirationDate,
                RelatedId = order.Id,
                Remark = $"订单 {order.OrderNo} 退款退货",
                TenantId = order.TenantId,
                TenantCode = order.TenantCode,
                StoreId = order.StoreId,
                StoreCode = order.StoreCode,
                CreatedTime = now
            });

            // 更新 OrderItemBatch.RefundedQuantity（B5.5 统计时用 Quantity - RefundedQuantity 计算实际销售）
            itemBatch.RefundedQuantity += refundQty;
            itemBatch.UpdatedTime = now;

            actions.Add($"商品(ID:{itemBatch.ProductId}) 批次 {itemBatch.BatchNo} 退库存 {refundQty:F4}");
        }
    }

    /// <summary>
    /// 历史订单退款兜底逻辑（无 OrderItemBatch 记录的订单）
    /// 按原逻辑退到最新在库批次，丢失效期追溯信息
    /// </summary>
    private async Task RefundRetailInventoryLegacyAsync(OrderEntity order, decimal ratio, List<string> actions, DateTime now)
    {
        var items = await _dbContext.OrderItems
            .Where(oi => oi.OrderId == order.Id)
            .ToListAsync();

        if (!items.Any())
        {
            actions.Add("零售订单无明细，跳过库存联动");
            return;
        }

        foreach (var item in items)
        {
            var returnQty = Math.Round(item.Quantity * ratio, 4);
            if (returnQty <= 0) continue;

            var batch = await _dbContext.InventoryBatches
                .Where(b => b.ProductId == item.ProductId && b.Status == 1 && b.TenantId == order.TenantId && b.StoreId == order.StoreId)
                .OrderByDescending(b => b.CreatedTime)
                .FirstOrDefaultAsync();

            var beforeQty = 0m;
            var inventory = await _dbContext.Inventories
                .FirstOrDefaultAsync(i => i.ProductId == item.ProductId && i.TenantId == order.TenantId && i.StoreId == order.StoreId);

            if (inventory != null)
            {
                beforeQty = inventory.Quantity;
                inventory.Quantity += returnQty;
                inventory.UpdatedTime = now;
            }
            else
            {
                inventory = new Inventory
                {
                    ProductId = item.ProductId,
                    Quantity = returnQty,
                    AlertQuantity = 0,
                    TenantId = order.TenantId,
                    TenantCode = order.TenantCode,
                    StoreId = order.StoreId,
                    StoreCode = order.StoreCode,
                    CreatedTime = now
                };
                _dbContext.Inventories.Add(inventory);
            }

            if (batch != null)
            {
                batch.Quantity += returnQty;
                batch.UpdatedTime = now;
            }
            else
            {
                batch = new InventoryBatch
                {
                    ProductId = item.ProductId,
                    BatchNo = $"RET-{order.OrderNo}-{item.ProductId}",
                    Quantity = returnQty,
                    UnitPrice = item.Price,
                    Status = 1,
                    Remark = $"订单 {order.OrderNo} 退款退货",
                    TenantId = order.TenantId,
                    TenantCode = order.TenantCode,
                    StoreId = order.StoreId,
                    StoreCode = order.StoreCode,
                    CreatedTime = now
                };
                _dbContext.InventoryBatches.Add(batch);
            }

            _dbContext.InventoryLogs.Add(new InventoryLog
            {
                ProductId = item.ProductId,
                Type = 1,
                SourceType = InventoryLogSourceTypes.ReturnInbound,
                UnitPrice = item.Price,
                Quantity = returnQty,
                BeforeQuantity = beforeQty,
                AfterQuantity = beforeQty + returnQty,
                BatchNo = batch.BatchNo,
                RelatedId = order.Id,
                Remark = $"订单 {order.OrderNo} 退款退货",
                TenantId = order.TenantId,
                TenantCode = order.TenantCode,
                StoreId = order.StoreId,
                StoreCode = order.StoreCode,
                CreatedTime = now
            });

            actions.Add($"商品 {item.ProductName}(ID:{item.ProductId}) 退库存 {returnQty:F4}（历史订单兜底）");
        }
    }

    /// <summary>
    /// 疗程卡核销订单退款：通过 TreatmentCardVerify.OrderId 反查核销记录，回退 RemainingTimes 和 TotalConsumedAmount，已用完则恢复为有效
    /// 仅处理疗程卡次数与金额的回退；库存/BOM 耗材回退由调用方（RefundAsync case 3 / ReverseOrderAsync）调用 RefundRetailInventoryAsync 统一处理
    /// （核销时按 Product.Type 联动扣减的库存/BOM 已写入 OrderItemBatch，可按批次精确退库）
    /// </summary>
    private async Task RefundTreatmentCardVerifyAsync(OrderEntity order, List<string> actions, DateTime now)
    {
        var verify = await _dbContext.TreatmentCardVerifies
            .FirstOrDefaultAsync(v => v.OrderId == order.Id && v.TenantId == order.TenantId);
        if (verify == null)
        {
            actions.Add("未找到关联的疗程卡核销记录，跳过疗程卡回退");
            return;
        }

        var sale = await _dbContext.TreatmentCardSales
            .FirstOrDefaultAsync(s => s.Id == verify.CardSaleId && s.TenantId == order.TenantId);
        if (sale == null)
        {
            actions.Add("未找到疗程卡销售记录，跳过疗程卡回退");
            return;
        }

        sale.RemainingTimes += verify.VerifyTimes;
        sale.TotalConsumedAmount -= verify.VerifyAmount;
        sale.UpdatedTime = now;
        // 已用完的疗程卡恢复为有效
        if (sale.Status == 2)
        {
            sale.Status = 1;
        }

        actions.Add($"疗程卡销售记录 ID:{sale.Id} 回退次数 {verify.VerifyTimes}，回退金额 {verify.VerifyAmount:F2}");
    }

    /// <summary>
    /// 组合支付订单退款（PayMethod=7）：按 CashAmount/StoredValueAmount/PointsAmount 比例分摊退款金额
    /// - 类别1（现金/支付宝/微信/银行卡）：线下退款，无系统联动，仅记录到 actions
    /// - 类别2（储值）：按比例分摊金额，退到 RealBalance（赠送余额不退），记 StoredValueLog
    /// - 类别3（积分）：按比例分摊金额，按 DeductRate 换算积分退还客户账户
    /// 最后一栏用减法补齐，避免分摊精度误差导致总和不等
    /// </summary>
    private async Task RefundCombinedPaymentAsync(OrderEntity order, decimal refundAmount, List<string> actions, DateTime now)
    {
        var cashAmount = order.CashAmount ?? 0m;
        var svAmount = order.StoredValueAmount ?? 0m;
        var pointsAmount = order.PointsAmount ?? 0m;
        var total = cashAmount + svAmount + pointsAmount;
        if (total <= 0)
        {
            actions.Add("组合支付三栏金额均为0，跳过退款分摊");
            return;
        }

        // 按比例分摊退款金额（前两栏按比例，最后一栏用减法补齐）
        var svRefund = Math.Round(refundAmount * (svAmount / total), 2, MidpointRounding.AwayFromZero);
        var pointsRefund = Math.Round(refundAmount * (pointsAmount / total), 2, MidpointRounding.AwayFromZero);
        var cashRefund = Math.Round(refundAmount - svRefund - pointsRefund, 2, MidpointRounding.AwayFromZero);

        // 类别1：线下退款，无系统联动
        if (cashRefund > 0)
        {
            actions.Add($"组合支付-类别1(现金类)退款 {cashRefund:F2}（线下/第三方退款，无系统联动）");
        }

        // 类别2：储值退款（退到 RealBalance，赠送余额不退）
        if (svRefund > 0)
        {
            await RefundStoredValueAsync(order, svRefund, actions, now);
        }

        // 类别3：积分退还（按 DeductRate 换算积分退还）
        if (pointsRefund > 0)
        {
            await RefundPointsPaymentAsync(order, pointsRefund, actions, now);
        }
    }

    /// <summary>
    /// 储值支付订单退款：退到 StoredValueAccount.RealBalance（赠送余额不退），更新 Balance 和 TotalConsume，记 StoredValueLog
    /// 当订单 PayMethod=5(储值卡支付)时，退款金额退还到客户储值账户的实收余额
    /// </summary>
    private async Task RefundStoredValueAsync(OrderEntity order, decimal refundAmount, List<string> actions, DateTime now)
    {
        if (!order.CustomerId.HasValue)
        {
            actions.Add("订单无客户ID，跳过储值退款");
            return;
        }

        var account = await _dbContext.StoredValueAccounts
            .FirstOrDefaultAsync(a => a.CustomerId == order.CustomerId.Value && a.TenantId == order.TenantId);
        if (account == null)
        {
            actions.Add("未找到客户储值账户，跳过储值退款");
            return;
        }

        var beforeBalance = account.Balance;
        var beforeRealBalance = account.RealBalance;
        var beforeGiftBalance = account.GiftBalance;

        // 仅退实收余额，赠送余额不退
        account.RealBalance += refundAmount;
        account.Balance += refundAmount;
        account.TotalConsume -= refundAmount;
        account.UpdatedTime = now;

        _dbContext.StoredValueLogs.Add(new StoredValueLog
        {
            CustomerId = account.CustomerId,
            Type = 3, // 退款
            Amount = refundAmount,
            RealAmount = refundAmount,
            GiftAmount = 0,
            BeforeBalance = beforeBalance,
            AfterBalance = beforeBalance + refundAmount,
            RealBalanceChange = refundAmount,
            GiftBalanceChange = 0,
            BeforeRealBalance = beforeRealBalance,
            AfterRealBalance = beforeRealBalance + refundAmount,
            BeforeGiftBalance = beforeGiftBalance,
            AfterGiftBalance = beforeGiftBalance,
            OrderId = order.Id,
            Remark = $"订单 {order.OrderNo} 退款",
            TenantId = order.TenantId,
            TenantCode = order.TenantCode,
            StoreId = order.StoreId,
            StoreCode = order.StoreCode,
            CreatedTime = now
        });

        actions.Add($"储值账户 ID:{account.Id} 退余额 {refundAmount:F2}（仅实收余额）");
    }

    /// <summary>
    /// 积分抵扣订单退款：按 DeductRate 将退款金额换算为积分退还客户账户
    /// 当订单 PayMethod=6(积分抵扣)时，退款金额按比例换算为积分退还
    /// </summary>
    private async Task RefundPointsPaymentAsync(OrderEntity order, decimal refundAmount, List<string> actions, DateTime now)
    {
        if (!order.CustomerId.HasValue)
        {
            actions.Add("订单无客户ID，跳过积分退还");
            return;
        }

        var rule = await GetEffectivePointsRuleAsync(order.TenantId, order.StoreId);
        if (rule == null || rule.DeductRate <= 0)
        {
            actions.Add("未找到启用的积分规则，跳过积分退还");
            return;
        }

        var customer = await _dbContext.Customers
            .FirstOrDefaultAsync(c => c.Id == order.CustomerId.Value && c.TenantId == order.TenantId);
        if (customer == null)
        {
            actions.Add("未找到客户，跳过积分退还");
            return;
        }

        // 退还积分按比例换算（向下取整，避免多退）
        var pointsToRefund = (int)Math.Floor(refundAmount / rule.DeductRate);
        if (pointsToRefund <= 0)
        {
            actions.Add($"退款金额 {refundAmount:F2} 不足1积分，跳过积分退还");
            return;
        }

        customer.TotalPoints += pointsToRefund;
        customer.UpdatedTime = now;
        actions.Add($"客户 ID:{customer.Id} 退还积分 {pointsToRefund}（退款 {refundAmount:F2} × 比例 1/{rule.DeductRate}）");
    }

    /// <summary>
    /// 积分扣减（内联实现，避免调用 CustomerPointsLogAppService.CreateAsync 破坏事务）
    /// 按比例计算扣减积分，积分不足时差额按 DeductRate 折算现金从退款扣除
    /// 返回积分不足折算扣除的现金金额（0 表示无折算）
    /// </summary>
    private async Task<decimal> RefundPointsAsync(OrderEntity order, decimal ratio, List<string> actions, DateTime now)
    {
        if (!order.CustomerId.HasValue)
        {
            actions.Add("订单无客户ID，跳过积分扣减");
            return 0;
        }

        var customer = await _dbContext.Customers
            .FirstOrDefaultAsync(c => c.Id == order.CustomerId.Value && c.TenantId == order.TenantId);
        if (customer == null)
        {
            actions.Add("未找到客户，跳过积分扣减");
            return 0;
        }

        // 按比例计算扣减积分（负数）
        var pointsToDeduct = -(int)Math.Round(order.Points * ratio);
        if (pointsToDeduct == 0) return 0;

        // 积分不足：扣减全部剩余积分，不足部分按 DeductRate 折算现金从退款扣除
        if (customer.TotalPoints + pointsToDeduct < 0)
        {
            var pointsRule = await GetEffectivePointsRuleAsync(order.TenantId, order.StoreId);
            var deductRate = pointsRule?.DeductRate ?? 0m;

            var shortfallPoints = Math.Abs(pointsToDeduct) - customer.TotalPoints;
            var cashDeduction = shortfallPoints * deductRate;

            var beforePoints = customer.TotalPoints;
            customer.TotalPoints = 0;
            customer.UpdatedTime = now;

            _dbContext.CustomerPointsLogs.Add(new CustomerPointsLog
            {
                CustomerId = customer.Id,
                Type = CustomerPointsLogType.RefundDeduct, // 退款扣减
                Points = -beforePoints,
                BeforePoints = beforePoints,
                AfterPoints = 0,
                OrderId = order.Id,
                OperatorId = _currentUser.UserId,
                Remark = $"订单 {order.OrderNo} 退款扣减积分（积分不足，扣减全部剩余 {beforePoints} 积分，不足 {shortfallPoints} 积分折算现金 {cashDeduction:F2} 元从退款扣除）",
                TenantId = order.TenantId,
                TenantCode = order.TenantCode,
                StoreId = order.StoreId,
                StoreCode = order.StoreCode,
                CreatedTime = now
            });

            actions.Add($"客户 ID:{customer.Id} 积分不足，扣减全部剩余 {beforePoints} 积分，不足 {shortfallPoints} 积分折算现金 {cashDeduction:F2} 元从退款扣除");
            return cashDeduction;
        }

        // 积分充足：正常扣减
        var beforePointsNormal = customer.TotalPoints;
        customer.TotalPoints += pointsToDeduct;
        customer.UpdatedTime = now;

        _dbContext.CustomerPointsLogs.Add(new CustomerPointsLog
        {
            CustomerId = customer.Id,
            Type = CustomerPointsLogType.RefundDeduct, // 退款扣减
            Points = pointsToDeduct,
            BeforePoints = beforePointsNormal,
            AfterPoints = customer.TotalPoints,
            OrderId = order.Id,
            OperatorId = _currentUser.UserId,
            Remark = $"订单 {order.OrderNo} 退款扣减积分",
            TenantId = order.TenantId,
            TenantCode = order.TenantCode,
            StoreId = order.StoreId,
            StoreCode = order.StoreCode,
            CreatedTime = now
        });

        actions.Add($"客户 ID:{customer.Id} 扣减积分 {pointsToDeduct}");
        return 0;
    }
}
