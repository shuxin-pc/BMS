using Mapster;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Bms.BuildingBlocks.Abstractions.Security;
using Bms.BuildingBlocks.Core.Context;
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
    private readonly IPointsRuleService _pointsRuleService;
    private readonly IPointsDeductionService _pointsDeductionService;
    private readonly ITechnicianStatisticAppService _technicianStatisticAppService;
    private readonly IAuditLogContext _auditLogContext;

    public OrderAppService(
        StoreDbContext dbContext,
        ICurrentUser currentUser,
        IValidator<OrderCreateDto> createValidator,
        IValidator<RefundRequestDto> refundValidator,
        IValidator<OrderCancelDto> cancelValidator,
        IInventoryAlertAppService alertAppService,
        IResourceConflictCheckService resourceConflictCheckService,
        IStoredValueAccountAppService storedValueAccountAppService,
        IDailySettlementAppService dailySettlementAppService,
        IPointsRuleService pointsRuleService,
        IPointsDeductionService pointsDeductionService,
        ITechnicianStatisticAppService technicianStatisticAppService,
        IAuditLogContext auditLogContext)
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
        _pointsRuleService = pointsRuleService;
        _pointsDeductionService = pointsDeductionService;
        _technicianStatisticAppService = technicianStatisticAppService;
        _auditLogContext = auditLogContext;
    }

    /// <summary>
    /// 获取订单分页列表
    /// 左连接 Customer（散客订单 CustomerId 为 null）和 Store，填充客户姓名、手机号、门店名称
    /// 并从 OrderItems 聚合消费项目摘要
    /// </summary>
    public async Task<ApiResponseDto<PagedResponseDto<OrderDto>>> GetPagedListAsync(OrderQueryDto query)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<PagedResponseDto<OrderDto>>.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;

        var queryable = from o in _dbContext.Orders
                        where o.TenantId == tenantId
                        join c in _dbContext.Customers on o.CustomerId equals c.Id into customers
                        from c in customers.DefaultIfEmpty()
                        join s in _dbContext.Stores on o.StoreId equals s.Id into stores
                        from s in stores.DefaultIfEmpty()
                        select new { o, c, s };

        if (!string.IsNullOrWhiteSpace(query.OrderNo))
            queryable = queryable.Where(x => x.o.OrderNo.Contains(query.OrderNo));
        if (query.CustomerId.HasValue)
            queryable = queryable.Where(x => x.o.CustomerId == query.CustomerId.Value);
        if (!string.IsNullOrWhiteSpace(query.Keyword))
            queryable = queryable.Where(x => x.c != null && (x.c.Name.Contains(query.Keyword) || x.c.Phone.Contains(query.Keyword)));
        if (query.OrderType.HasValue)
            queryable = queryable.Where(x => x.o.OrderType == query.OrderType.Value);
        if (query.Status.HasValue)
            queryable = queryable.Where(x => x.o.Status == query.Status.Value);
        if (query.PayMethod.HasValue)
            queryable = queryable.Where(x => x.o.PayMethod == query.PayMethod.Value);
        if (query.BackfillStatus.HasValue)
            queryable = queryable.Where(x => x.o.BackfillStatus == query.BackfillStatus.Value);
        if (query.StartDate.HasValue)
            queryable = queryable.Where(x => x.o.OrderTime >= query.StartDate.Value);
        if (query.EndDate.HasValue)
            queryable = queryable.Where(x => x.o.OrderTime < query.EndDate.Value.AddDays(1));

        var total = await queryable.CountAsync();
        var items = await queryable
            .OrderByDescending(x => x.o.CreatedTime)
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(x => new OrderDto
            {
                Id = x.o.Id,
                OrderNo = x.o.OrderNo,
                CustomerId = x.o.CustomerId,
                CustomerName = x.c != null ? x.c.Name : null,
                Phone = x.c != null ? x.c.Phone : null,
                StoreName = x.s != null ? x.s.Name : null,
                ProjectSummary = string.Join("、", x.o.OrderItems.Select(oi => oi.ProductName).Distinct()),
                OrderType = x.o.OrderType,
                Status = x.o.Status,
                BackfillStatus = x.o.BackfillStatus,
                ProductAmount = x.o.ProductAmount,
                DiscountAmount = x.o.DiscountAmount,
                PaidAmount = x.o.PaidAmount,
                Points = x.o.Points,
                PayMethod = x.o.PayMethod,
                CashAmount = x.o.CashAmount,
                CashPayMethod = x.o.CashPayMethod,
                StoredValueAmount = x.o.StoredValueAmount,
                PointsAmount = x.o.PointsAmount,
                DeductRate = x.o.DeductRate,
                OrderTime = x.o.OrderTime,
                CompleteTime = x.o.CompleteTime,
                RefundAmount = x.o.RefundAmount,
                RefundTime = x.o.RefundTime,
                RefundReason = x.o.RefundReason,
                OperatorId = x.o.OperatorId,
                Remark = x.o.Remark,
                CreatedAt = x.o.CreatedTime,
                UpdatedAt = x.o.UpdatedTime
            })
            .ToListAsync();

        var result = new PagedResponseDto<OrderDto>
        {
            List = items,
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
            return ApiResponseDto<List<OrderDto>>.Fail("登录状态异常，请重新登录", 401);

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
            return ApiResponseDto<OrderDto?>.Fail("登录状态异常，请重新登录", 401);

        var entity = await _dbContext.Orders
            .Include(o => o.OrderItems).ThenInclude(oi => oi.Batches)
            .FirstOrDefaultAsync(o => o.Id == id && o.TenantId == _currentUser.TenantId.Value);
        if (entity == null)
            return ApiResponseDto<OrderDto?>.Fail("订单不存在", 404);

        var dto = entity.Adapt<OrderDto>();

        // 关联查询商品类型填充到明细（Product.Master.Type：1实物/2服务/3耗材/4样品/5赠品）
        // 必须用 Select 表达式投影（EF 翻译为 JOIN，在数据库端取值），不能在 ToDictionaryAsync 的 Func 委托里访问 Master 导航
        var detailProductIds = dto.Items.Select(i => i.ProductId).Distinct().ToList();
        if (detailProductIds.Any())
        {
            var productTypeMap = (await _dbContext.Products
                    .Where(p => detailProductIds.Contains(p.Id) && !p.IsDeleted)
                    .Select(p => new { p.Id, MasterType = p.Master != null ? p.Master.Type : (int?)null })
                    .ToListAsync())
                .ToDictionary(m => m.Id, m => m.MasterType);
            foreach (var item in dto.Items)
            {
                if (productTypeMap.TryGetValue(item.ProductId, out var masterType) && masterType.HasValue)
                    item.ProductType = masterType.Value;
            }
        }

        // 关联查询客户姓名填充（散客订单 CustomerId 为空，保持 null）
        if (entity.CustomerId.HasValue)
        {
            dto.CustomerName = await _dbContext.Customers
                .Where(c => c.Id == entity.CustomerId.Value)
                .Select(c => c.Name)
                .FirstOrDefaultAsync();
        }

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

        // 关联查询批次商品名称填充（退款弹窗展示可退批次所属商品：
        // 服务项目/项目卡核销行的 OrderItemBatch.ProductId 是 BOM 耗材，此处填充的是耗材名称）
        var batchProductIds = dto.Items
            .SelectMany(i => i.Batches)
            .Select(b => b.ProductId)
            .Distinct()
            .ToList();
        if (batchProductIds.Any())
        {
            var batchProductNames = (await _dbContext.Products
                    .Where(p => batchProductIds.Contains(p.Id) && !p.IsDeleted)
                    .Select(p => new { p.Id, MasterName = p.Master != null ? p.Master.Name : string.Empty })
                    .ToListAsync())
                .ToDictionary(m => m.Id, m => m.MasterName);
            foreach (var item in dto.Items)
            {
                foreach (var batch in item.Batches)
                {
                    if (batchProductNames.TryGetValue(batch.ProductId, out var name))
                        batch.ProductName = name;
                }
            }
        }

        return ApiResponseDto<OrderDto?>.Ok(dto);
    }

    /// <summary>
    /// 创建订单（事务包裹，按 OrderType 联动库存/项目卡，按 PayMethod 联动储值）
    /// OrderType=1零售: 扣减实物商品库存（按效期选择批次）
    /// OrderType=2服务: 扣减BOM耗材库存（FEFO自动选择）
    /// OrderType=3项目卡核销: 核销项目卡+附加零售商品出库
    /// PayMethod=5储值支付: 扣减储值余额（先实收后赠送，不发积分）
    /// 积分规则: PayMethod≠5且OrderType≠3时按PointsRate计算，Floor取整
    /// </summary>
    public async Task<ApiResponseDto<OrderDto>> CreateAsync(OrderCreateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<OrderDto>.Fail("登录状态异常，请重新登录", 401);

        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<OrderDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var tenantCode = _currentUser.TenantCode ?? string.Empty;

        if (dto.Items == null || !dto.Items.Any())
            return ApiResponseDto<OrderDto>.Fail("订单明细不能为空", 400);

        // 项目卡核销订单（OrderType=3）必须通过 TreatmentCardVerifyAppService.CreateAsync 创建
        // 该方法负责：① 创建核销订单 ② 扣减项目卡次数 ③ 按 Product.Type 联动扣库存/BOM（见 P-TC-01）
        // 此处禁止 OrderType=3 的入口，避免遗漏库存/BOM 扣减
        if (dto.OrderType == 3)
            return ApiResponseDto<OrderDto>.Fail("项目卡核销请通过核销接口创建", 400);

        // 不可销售品项中，仅 Type=4 样品仍禁止下单（必须走样品领用流程）
        // Type=5 赠品允许进入订单，走赠品出库路径（DeductSampleGiftOutAsync，详见第 1 步库存扣减）
        // 前端已保证赠品 Price=0；ExpirationDates 可选（未指定时后端按 FEFO 自动扣减，与实物商品一致），后端不做这些业务校验
        var orderProductIds = dto.Items.Select(i => i.ProductId).Distinct().ToList();
        // 商品类型按主档 Type 取值：必须用 Select 表达式投影（EF 翻译为 JOIN，在数据库端取值）
        // 不能在 ToDictionaryAsync 的 Func 委托里访问 Master 导航——导航未 Include 加载时恒为 null，会抛空引用
        var productTypeMap = (await _dbContext.Products
                .Where(p => orderProductIds.Contains(p.Id) && !p.IsDeleted)
                .Select(p => new { p.Id, MasterType = p.Master != null ? p.Master.Type : (int?)null })
                .ToListAsync())
            .ToDictionary(m => m.Id, m => m.MasterType!.Value);

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
        // 赠品走 DeductSampleGiftOutAsync（仅写 InventoryLog SourceType=GiftOutbound）
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
                .FirstOrDefaultAsync(a => a.Id == dto.SourceAppointmentId.Value && a.TenantId == tenantId && a.StoreId == (_currentUser.StoreId ?? 0));
            if (sourceAppointment == null)
                return ApiResponseDto<OrderDto>.Fail($"源预约 {dto.SourceAppointmentId} 不存在", 400);

            // 预约转单结算校验：源预约必须可流转为已完成（1已预约/2已到店 可转单；4取消/5爽约 终态禁止转单）
            // 已在事务开始前校验，失败直接返回无需回滚；已完成(3) 时幂等放行，下方不重复处理
            if (!AppointmentStatusTransition.CanTransition(sourceAppointment.Status, AppointmentStatus.Completed))
                return ApiResponseDto<OrderDto>.Fail(
                    $"源预约当前状态({sourceAppointment.Status})不能转单结算，仅 1已预约/2已到店 可转单", 400);

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
                // OrderItem 未指定服务时间时，从源预约继承（前端可显式覆盖）
                // 服务开始 = 预约开始时间 StartTime，服务结束 = 预约 EndTime（预约创建时按服务时长自动计算，支持跨日）
                if (!item.ServiceStartTime.HasValue)
                {
                    item.ServiceStartTime = sourceAppointment.StartTime;
                    item.ServiceEndTime = sourceAppointment.EndTime;
                }
            }
        }

        // 服务订单（OrderType=2）资源冲突检测（技师/房间/设备维度，仅预约占用）
        // 已入库订单（Status=2 创建即完成）服务已结束、资源已释放，不参与占用
        // 预约转订单场景已由源预约保证占用，跳过验证
        if (dto.OrderType == 2 && !dto.SourceAppointmentId.HasValue)
        {
            // 预加载每个 OrderItem 的服务时长（按 ProductId 聚合），用于计算占用结束时间
            // OrderItem.ProductId 是门店商品档案 Product.Id，需经 Product.MasterId 关联 ServiceProduct.MasterId
            var productIds = dto.Items.Select(i => i.ProductId).Distinct().ToList();
            var productMasterMap = await _dbContext.Products
                .Where(p => productIds.Contains(p.Id))
                .Select(p => new { p.Id, p.MasterId })
                .ToDictionaryAsync(p => p.Id, p => p.MasterId);
            var masterIds = productMasterMap.Values.Distinct().ToList();
            var durationsByMaster = await _dbContext.ServiceProducts
                .Where(sp => masterIds.Contains(sp.MasterId))
                .ToDictionaryAsync(sp => sp.MasterId, sp => sp.Duration ?? 0);
            var productDurations = productMasterMap
                .Where(kv => durationsByMaster.TryGetValue(kv.Value, out _))
                .ToDictionary(kv => kv.Key, kv => durationsByMaster[kv.Value]);

            var orderStart = dto.OrderTime == default ? now : dto.OrderTime;

            // 按技师/房间/设备聚合占用区间
            var conflicts = new List<string>();
            var conflictedResources = new HashSet<string>();
            foreach (var item in dto.Items)
            {
                if (!productDurations.TryGetValue(item.ProductId, out var duration) || duration <= 0)
                {
                    // 服务项目未配置时长，无法判定占用区间，跳过冲突检测
                    continue;
                }
                // 技师/房间/设备三项资源全空时无占用维度，跳过冲突检测
                if (!item.TechnicianId.HasValue && !item.RoomId.HasValue && !item.EquipmentId.HasValue)
                {
                    continue;
                }
                // 占用时段优先用真实服务时间（服务内容弹窗录入），为空回退 下单时间 + 服务时长 推算
                // ServiceStartTime 有值而 ServiceEndTime 为空时按 ServiceStartTime + duration 补全
                var itemStart = item.ServiceStartTime ?? orderStart;
                var itemEnd = item.ServiceEndTime ?? itemStart.AddMinutes(duration);

                var result = await _resourceConflictCheckService.CheckAsync(
                    tenantId, _currentUser.StoreId ?? 0, item.TechnicianId, item.RoomId, item.EquipmentId,
                    itemStart, itemEnd);

                if (result.HasAnyConflict)
                {
                    if (result.TechnicianConflict)
                    {
                        conflicts.Add($"您选择的技师在该时段已有安排（{result.TechnicianConflictInfo}）");
                        conflictedResources.Add("技师");
                    }
                    if (result.RoomConflict)
                    {
                        conflicts.Add($"您选择的房间在该时段已有安排（{result.RoomConflictInfo}）");
                        conflictedResources.Add("房间");
                    }
                    if (result.EquipmentConflict)
                    {
                        conflicts.Add($"您选择的设备在该时段已有安排（{result.EquipmentConflictInfo}）");
                        conflictedResources.Add("设备");
                    }
                }
            }
            if (conflicts.Any())
            {
                var advice = $"请更换{string.Join("、", conflictedResources)}或调整服务时间";
                return ApiResponseDto<OrderDto>.Fail($"{string.Join("；", conflicts.Distinct())}，{advice}", 400);
            }
        }

        // 映射实体并设置审计字段
        var entity = dto.Adapt<OrderEntity>();
        entity.TenantId = tenantId;
        entity.TenantCode = tenantCode;
        // 门店归属：取自当前登录用户门店（对齐 Appointment/PurchaseOrder/InventoryBatch 等实体统一模式）
        // 保证：① 单号按"同租户+同门店+当日"维度生成且数据库组合唯一索引不冲突；
        //       ② 库存扣减按 StoreId 匹配批次（InventoryBatch.StoreId 同样取自 _currentUser.StoreId）；
        //       ③ 日结/统计按真实门店过滤订单。
        entity.StoreId = _currentUser.StoreId ?? 0;
        entity.StoreCode = _currentUser.StoreCode ?? string.Empty;
        entity.CreatedTime = now;
        entity.OrderTime = dto.OrderTime == default ? now : dto.OrderTime;
        // 纯补录模式：门店先完成服务再录入系统，所有订单创建即完成（Status=2）
        // OrderType=1零售 / OrderType=2服务（含混合单）统一已完成，无"进行中+完成按钮"闭环
        // 资源占用检测范围已扩为 Status∈{1,2}（见 ResourceConflictCheckService），不再依赖 Status=1 标记占用
        entity.Status = 2; // 已完成
        entity.CompleteTime = now;

        // 设置明细审计字段（门店归属与订单头保持一致）
        foreach (var item in entity.OrderItems)
        {
            item.TenantId = tenantId;
            item.TenantCode = tenantCode;
            item.StoreId = entity.StoreId;
            item.StoreCode = entity.StoreCode;
            item.CreatedTime = now;
        }

        // 混合结算（PosCheckoutAppService）调用时复用其外部事务，本方法不自开/不提交/不回滚；
        // 独立下单时自开事务（原行为不变）。事务级顾问锁在外部事务内同样生效（随外层提交/回滚释放）
        var ownsTransaction = _dbContext.Database.CurrentTransaction == null;
        await using var transaction = ownsTransaction
            ? await _dbContext.Database.BeginTransactionAsync()
            : null;
        try
        {
            // 订单号由后端生成（SO{yyyyMMdd}{序号}，同租户+门店+当日递增）
            // 事务级顾问锁串行化并发请求，避免"查max+1"在并发下产生重复单号
            var storeId = entity.StoreId;
            var lockKey = OrderNoGenerator.BuildLockKey(tenantId, storeId, entity.OrderTime);
            await _dbContext.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock({0})", lockKey);
            entity.OrderNo = await OrderNoGenerator.GenerateAsync(_dbContext, tenantId, storeId, entity.OrderTime);

            _dbContext.Orders.Add(entity);
            await _dbContext.SaveChangesAsync();

            // 1. 按 OrderType 联动库存/项目卡
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
                case 2: // 服务：按行分流扣减——服务行扣 BOM 耗材，实物行(Type=1)补扣实物库存，赠品行走赠品出库
                    // 混合订单（购物车同时含零售商品与服务项目）整体记为 OrderType=2，
                    // 原实现仅扣 BOM 耗材导致零售商品实物库存不扣（混合单丢库存 bug，WP1）
                    await DeductServiceBomAsync(entity, dto.Items, now);
                    await DeductRetailInventoryInServiceAsync(entity, dto.Items, productTypeMap, giftProductIds, now);
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

            // 3. 积分发放（PayMethod=5储值支付、PayMethod=6积分抵扣、OrderType=3项目卡核销不发积分）
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

            // 5. 记录商品销售统计（ProductSalesStat，跳过 Type=5 赠品，按商品实际类型按行映射）
            await RecordProductSalesStatAsync(entity, productTypeMap, giftProductIds, now);

            // 6. 补录订单跨日触发原下单日反日结（方案 B，2026-07-19）
            // 补录订单(BackfillStatus=1)下单日早于今日时，自动反日结原下单日并重算
            // 与退款/取消订单保持一致的跨日变动处理口径
            List<DateTime> reversedDates = new();
            if (entity.BackfillStatus == 1 && entity.OrderTime.Date < DateTime.Today)
            {
                var reverseReason = $"补录订单自动反日结：订单 {entity.OrderNo}（实付 {entity.PaidAmount:F2} 元）";
                reversedDates = await ReverseSettlementForOrderReversalAsync(entity, reverseReason, now);
            }

            // 预约转单结算成功：源预约同步标记为已完成（客户到店消费完成即视为预约服务完成）
            // 源预约已被 EF 跟踪，此处属性修改随本次 SaveChanges 持久化；与订单同一 DB 事务提交/回滚，
            // 结算失败时预约状态随之回滚，保证数据一致，并杜绝同一预约被重复转单
            if (sourceAppointment != null)
            {
                sourceAppointment.Status = AppointmentStatus.Completed;
                sourceAppointment.CompleteTime = now;
            }

            await _dbContext.SaveChangesAsync();

            // 技师统计归集：服务订单（OrderType=2）创建即完成（Status=2），按明细涉及的 (技师, 服务日期) 重算归集
            // 在 Commit 前调用，归集写入与订单同事务，失败则整体回滚保证一致性
            if (dto.OrderType == 2)
            {
                await RecalculateTechnicianStatsForOrderAsync(entity);
            }

            if (ownsTransaction)
                await transaction!.CommitAsync();
            // 仅自开事务时提交；外部事务由混合结算统一提交

            // 库存变动后即时检测低库存预警
            await CheckAlertsAfterInventoryChangeAsync(entity.Id, tenantId, entity.StoreId);

            var message = reversedDates.Any()
                ? $"创建成功，订单下单日 {string.Join("、", reversedDates.Select(d => d.ToString("yyyy-MM-dd")))} 日结已自动反日结/补建，请前往日结管理重新确认"
                : "创建成功";
            return ApiResponseDto<OrderDto>.Ok(entity.Adapt<OrderDto>(), message);
        }
        catch (InvalidOperationException ex) when (ex.Message.StartsWith("INSUFFICIENT_EXPIRY_STOCK"))
        {
            // 选中效期库存不足：回滚事务，返回 409 让前端弹窗让店员决定是否自动补足
            // 混合结算场景下不回滚外部事务（由 PosCheckoutAppService 统一回滚）
            if (ownsTransaction)
                await transaction!.RollbackAsync();
            return ApiResponseDto<OrderDto>.Fail(ex.Message, 409);
        }
        catch
        {
            if (ownsTransaction)
                await transaction!.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// 库存变动后即时检测低库存和积压预警（通过 InventoryLog 查找涉及的商品）
    /// </summary>
    private async Task CheckAlertsAfterInventoryChangeAsync(long orderId, long tenantId, long storeId)
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
                await _alertAppService.CheckInventoryAlertsAsync(tenantId, storeId, productId);
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
                InventoryLogSourceTypes.SalesOutbound, activityId: null);
        }
    }

    /// <summary>
    /// 服务订单（OrderType=2）按行分流补扣实物库存：遍历 dto.Items，仅对实物行（ProductMaster.Type=1）调用
    /// DeductItemBatchesAsync 扣销售出库库存。混合订单（购物车同时含零售商品与服务项目）整体记为 OrderType=2，
    /// 服务行走 DeductServiceBomAsync 扣 BOM 耗材，赠品行（Type=5）走 DeductSampleGiftOutAsync，实物行在本方法补扣，
    /// 修复混合单零售商品丢库存问题（WP1）。其他类型（耗材/样品等）不参与订单销售扣减，跳过。
    /// </summary>
    private async Task DeductRetailInventoryInServiceAsync(
        OrderEntity order, List<OrderItemCreateDto> dtoItems,
        Dictionary<long, int> productTypeMap, HashSet<long> giftProductIds, DateTime now)
    {
        for (var i = 0; i < dtoItems.Count; i++)
        {
            var dtoItem = dtoItems[i];
            // 赠品单独出库（DeductSampleGiftOutAsync），此处跳过
            if (giftProductIds.Contains(dtoItem.ProductId)) continue;
            // 仅实物行(Type=1)补扣实物库存；服务行/耗材/样品等不在此扣减
            if (!productTypeMap.TryGetValue(dtoItem.ProductId, out var masterType) || masterType != 1) continue;

            var item = order.OrderItems[i];
            await DeductItemBatchesAsync(order, item, dtoItem, now,
                InventoryLogSourceTypes.SalesOutbound, activityId: null);
        }
    }

    /// <summary>
    /// 赠品出库扣减：遍历 dto.Items 中的 Type=5 赠品，调用 DeductItemBatchesAsync 按效期扣减批次库存。
    /// 解耦 SampleGiftOut 二次记录（R4），仅保留批次扣减 + InventoryLog(SourceType=GiftOutbound) 流水。
    /// 赠品项可选关联活动（dtoItem.ActivityId），传入时校验活动存在且未删除，写入 InventoryLog.ActivityId。
    /// </summary>
    private async Task DeductSampleGiftOutAsync(
        OrderEntity order, List<OrderItemCreateDto> dtoItems, HashSet<long> giftProductIds, DateTime now)
    {
        for (var i = 0; i < dtoItems.Count; i++)
        {
            var dtoItem = dtoItems[i];
            if (!giftProductIds.Contains(dtoItem.ProductId)) continue;

            // 校验赠品项关联活动：传入时验证活动存在且未删除（软删除活动历史流水仍可显示）
            if (dtoItem.ActivityId.HasValue)
            {
                var activityExists = await _dbContext.Activities
                    .AnyAsync(a => a.Id == dtoItem.ActivityId.Value && !a.IsDeleted
                        && a.TenantId == order.TenantId && a.StoreId == order.StoreId);
                if (!activityExists)
                    throw new InvalidOperationException($"赠品关联活动不存在（活动ID：{dtoItem.ActivityId}）");
            }

            var item = order.OrderItems[i];
            await DeductItemBatchesAsync(order, item, dtoItem, now,
                InventoryLogSourceTypes.GiftOutbound, activityId: dtoItem.ActivityId);
        }
    }

    /// <summary>
    /// 单个 OrderItem 的批次选择与扣减（DeductRetailInventoryAsync 与 DeductSampleGiftOutAsync 共享）。
    /// 按店员选择的效期顺序优先扣减，同效期多批次按 PurchaseDate ASC（FIFO）逐批扣减。
    /// 店员未选效期或允许自动补足时，按 FEFO（近效期优先）自动扣减。
    /// 选中效期不足且 AllowAutoFillBeyondSelection=false 时抛带 INSUFFICIENT_EXPIRY_STOCK 前缀的异常，
    /// 由 CreateAsync 捕获并返回 409，前端弹窗让店员决定是否自动从近效期补足。
    /// 通过 sourceType 区分 InventoryLog.SourceType，通过 activityId 写入 InventoryLog.ActivityId（赠品出库路径传值，销售出库传 null）。
    /// </summary>
    private async Task DeductItemBatchesAsync(
        OrderEntity order, OrderItem item, OrderItemCreateDto dtoItem, DateTime now,
        int sourceType, long? activityId)
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

                remaining = await DeductFromBatchesAsync(order, item, batches, remaining, now, sourceType, activityId);
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

            remaining = await DeductFromBatchesAsync(order, item, fefoBatches, remaining, now, sourceType, activityId);
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
    /// 通过 activityId 写入 InventoryLog.ActivityId（赠品出库路径传值，销售出库传 null）。
    /// </summary>
    private async Task<decimal> DeductFromBatchesAsync(
        OrderEntity order, OrderItem item, List<InventoryBatch> batches, decimal needQty, DateTime now,
        int sourceType, long? activityId)
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
            // P-SG-02：赠品出库统一使用 GiftOutbound（R2 后 SampleGiftOuts 表已删除，仅写 InventoryLog）
            var remark = order.OrderNo;
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
                ActivityId = activityId,
                Remark = remark,
                OperatorId = _currentUser.UserId,
                OperatorName = _currentUser.RealName ?? _currentUser.UserName,
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

            remaining -= deduct;
        }

        return remaining;
    }

    /// <summary>
    /// 服务耗材出库：按BOM计算耗材需求扣减批次库存。
    /// 店员在快速开单加购服务项目时选择了绑定耗材效期（OrderItemCreateDto.ConsumableExpiries）时按指定效期扣减；
    /// 未选择（未绑定耗材或默认自动推荐）时按 FEFO（近效期优先）自动扣减。
    /// 指定效期不足且不允许自动补足时抛 INSUFFICIENT_EXPIRY_STOCK 409（与实物商品一致，前端弹窗让店员决定补足）；
    /// 未指定效期库存不足时抛异常触发事务回滚。
    /// 扣减明细记录到 OrderItemBatch（替代原 ConsumableDeduction JSON），支持效期追溯和成本归集
    /// </summary>
    private async Task DeductServiceBomAsync(OrderEntity order, List<OrderItemCreateDto> dtoItems, DateTime now)
    {
        for (var i = 0; i < order.OrderItems.Count; i++)
        {
            var item = order.OrderItems[i];
            var dtoItem = dtoItems[i];

            // 查找服务项目的BOM：BOM 服务端关联服务项目档案，而订单项 ProductId 是门店商品档案，
            // 经商品主档桥接（商品档案.MasterId == 服务项目档案.MasterId）定位对应服务项目档案
            var serviceProductIds = await _dbContext.Products
                .Where(p => p.Id == item.ProductId)
                .Join(_dbContext.ServiceProducts, p => p.MasterId, sp => sp.MasterId, (p, sp) => sp.Id)
                .Distinct()
                .ToListAsync();
            var boms = serviceProductIds.Count == 0
                ? new List<ServiceBom>()
                : await _dbContext.ServiceBoms
                    .Where(b => serviceProductIds.Contains(b.ServiceProductId) && b.TenantId == order.TenantId)
                    .ToListAsync();

            if (!boms.Any()) continue;

            // 一次性加载本次涉及耗材的商品名（409 提示用）
            var consumableIds = boms.Select(b => b.ConsumableProductId).Distinct().ToList();
            var consumableNames = await _dbContext.Products
                .Where(p => consumableIds.Contains(p.Id))
                .Select(p => new { p.Id, Name = p.Master != null ? p.Master.Name : string.Empty })
                .ToDictionaryAsync(p => p.Id, p => p.Name);

            decimal totalConsumableCost = 0m;

            foreach (var bom in boms)
            {
                // 需要扣减的数量 = BOM单次消耗量 × 订单数量
                var needQty = bom.Quantity * item.Quantity;

                // 店员为该耗材选择的效期（未绑定耗材/默认自动推荐时为空，走 FEFO）
                var expiry = dtoItem.ConsumableExpiries
                    .FirstOrDefault(e => e.ProductId == bom.ConsumableProductId);

                // 按指定效期扣减（不足走 409 补足）；未指定效期按 FEFO 自动扣减
                totalConsumableCost += await DeductConsumableBatchesAsync(
                    order, item, bom.ConsumableProductId,
                    consumableNames.GetValueOrDefault(bom.ConsumableProductId, $"耗材(ID:{bom.ConsumableProductId})"),
                    needQty, expiry?.ExpirationDates, dtoItem.AllowAutoFillBeyondSelection, now);
            }

            // 归集耗材成本到服务项目（OrderItemBatch 已结构化存储明细，无需 JSON 序列化）
            if (totalConsumableCost > 0m)
            {
                item.ConsumableCost = totalConsumableCost;
            }
        }
    }

    /// <summary>
    /// 服务 BOM 耗材批次扣减（DeductServiceBomAsync 逐耗材调用）。
    /// 店员选择了效期时按选择顺序优先扣减（同效期 FIFO）；未选效期或允许自动补足时按 FEFO 自动扣减（排除已扣减效期）。
    /// 选中效期不足且 AllowAutoFillBeyondSelection=false 时抛带 INSUFFICIENT_EXPIRY_STOCK 前缀的异常（409），
    /// 由 CreateAsync 捕获返回 409，前端弹窗让店员决定是否自动从近效期补足。
    /// 扣减明细写入 OrderItemBatch（ProductId=耗材ID，便于按耗材统计效期消耗），返回扣减耗材成本合计。
    /// </summary>
    private async Task<decimal> DeductConsumableBatchesAsync(
        OrderEntity order, OrderItem item, long consumableProductId, string consumableName,
        decimal needQty, List<DateTime?>? expirationDates, bool allowAutoFill, DateTime now)
    {
        var remaining = needQty;
        var totalCost = 0m;
        // 记录已扣减的效期日期（自动补足时排除）；null 表示"无效期限制"批次
        var processedExpirationDates = new List<DateTime?>();

        // 1. 店员选择了效期 -> 按选择顺序扣减
        if (expirationDates != null && expirationDates.Any())
        {
            foreach (var expirationDate in expirationDates)
            {
                if (remaining <= 0) break;

                List<InventoryBatch> batches;
                if (expirationDate.HasValue)
                {
                    batches = await _dbContext.InventoryBatches
                        .Where(b => b.ProductId == consumableProductId
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
                        .Where(b => b.ProductId == consumableProductId
                            && !b.ExpirationDate.HasValue
                            && b.Status == 1
                            && b.Quantity > 0
                            && b.TenantId == order.TenantId
                            && b.StoreId == order.StoreId)
                        .OrderBy(b => b.CreatedTime) // 无效期批次按 CreatedTime 升序
                        .ToListAsync();
                }

                var (r, c) = await DeductFromConsumableBatchesAsync(order, item, consumableProductId, batches, remaining, now);
                remaining = r;
                totalCost += c;
                processedExpirationDates.Add(expirationDate);
            }
        }

        // 2. 未选效期 OR 允许自动补足 -> 系统按 FEFO 自动扣减（排除已扣减的效期）
        if (remaining > 0
            && (expirationDates == null || !expirationDates.Any() || allowAutoFill))
        {
            var fefoQuery = _dbContext.InventoryBatches
                .Where(b => b.ProductId == consumableProductId
                    && b.Status == 1
                    && b.Quantity > 0
                    && b.TenantId == order.TenantId
                    && b.StoreId == order.StoreId);

            if (processedExpirationDates.Any())
            {
                var processedDates = processedExpirationDates
                    .Where(d => d.HasValue)
                    .Select(d => d!.Value.Date)
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

            var (r2, c2) = await DeductFromConsumableBatchesAsync(order, item, consumableProductId, fefoBatches, remaining, now);
            remaining = r2;
            totalCost += c2;
        }

        // 3. 仍有剩余 = 库存不足
        if (remaining > 0)
        {
            if (expirationDates != null && expirationDates.Any() && !allowAutoFill)
            {
                // 选中效期不足且未允许自动补足 -> 构造 409 结构化错误（与实物商品 DeductItemBatchesAsync 格式一致）
                var availableBatches = await _dbContext.InventoryBatches
                    .Where(b => b.ProductId == consumableProductId
                        && b.Status == 1
                        && b.Quantity > 0
                        && b.TenantId == order.TenantId
                        && b.StoreId == order.StoreId)
                    .ToListAsync();

                var availableOptions = availableBatches
                    .GroupBy(b => b.ExpirationDate.HasValue ? (DateTime?)b.ExpirationDate.Value.Date : null)
                    .Select(g => new { ExpirationDate = g.Key, TotalQuantity = g.Sum(b => b.Quantity) })
                    .OrderBy(x => x.ExpirationDate.HasValue ? 0 : 1)  // null 批次排末尾
                    .ThenBy(x => x.ExpirationDate)                    // 近效期优先
                    .ToList();

                var optionsStr = string.Join(";",
                    availableOptions.Select(x =>
                        x.ExpirationDate.HasValue
                            ? $"{x.ExpirationDate:yyyy-MM-dd}:{x.TotalQuantity}"
                            : $"无:{x.TotalQuantity}"));

                throw new InvalidOperationException(
                    $"INSUFFICIENT_EXPIRY_STOCK|{consumableProductId}|{consumableName}|{remaining}|{optionsStr}");
            }

            throw new InvalidOperationException($"耗材 {consumableName} 库存不足，还需 {remaining}");
        }

        return totalCost;
    }

    /// <summary>
    /// 从指定耗材批次列表中逐批扣减库存，更新 Inventory 汇总表，记 InventoryLog 与 OrderItemBatch。
    /// ProductId 记录耗材ID（bom.ConsumableProductId），便于按耗材统计效期消耗。
    /// 返回 (未满足剩余数量, 扣减耗材成本合计)。
    /// </summary>
    private async Task<(decimal Remaining, decimal Cost)> DeductFromConsumableBatchesAsync(
        OrderEntity order, OrderItem item, long consumableProductId,
        List<InventoryBatch> batches, decimal needQty, DateTime now)
    {
        var remaining = needQty;
        var cost = 0m;

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
                .FirstOrDefaultAsync(inv => inv.ProductId == consumableProductId && inv.TenantId == order.TenantId && inv.StoreId == order.StoreId);
            if (inventory != null)
            {
                inventory.Quantity -= deduct;
                inventory.UpdatedTime = now;
            }

            // 记录流水
            _dbContext.InventoryLogs.Add(new InventoryLog
            {
                ProductId = consumableProductId,
                Type = 2,
                SourceType = InventoryLogSourceTypes.SalesOutbound,
                UnitPrice = batch.UnitPrice,
                Quantity = -deduct,
                BeforeQuantity = beforeQty,
                AfterQuantity = batch.Quantity,
                BatchNo = batch.BatchNo,
                ExpirationDate = batch.ExpirationDate,
                RelatedId = order.Id,
                Remark = order.OrderNo,
                OperatorId = _currentUser.UserId,
                OperatorName = _currentUser.RealName ?? _currentUser.UserName,
                TenantId = order.TenantId,
                TenantCode = order.TenantCode,
                StoreId = order.StoreId,
                StoreCode = order.StoreCode,
                CreatedTime = now
            });

            // 记录订单明细批次扣减（OrderItemBatch：效期追溯和统计的数据源）
            _dbContext.OrderItemBatches.Add(new OrderItemBatch
            {
                OrderItemId = item.Id,
                OrderId = order.Id,
                ProductId = consumableProductId,
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

            cost += deduct * batch.UnitPrice;
            remaining -= deduct;
        }

        return (remaining, cost);
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
    /// 积分抵扣：委托 IPointsDeductionService 按 PointsRule.DeductRate 计算并扣减客户积分、记录兑换与流水
    /// 扣减/记录逻辑已抽离至共享服务（订单与项目卡开卡共用），此处仅补充订单特有的累计消费更新
    /// </summary>
    /// <param name="amount">抵扣金额（单一积分支付时为 order.PaidAmount；组合支付时为 PointsAmount）</param>
    private async Task DeductPointsAsync(OrderEntity order, decimal amount, DateTime now)
    {
        if (!order.CustomerId.HasValue)
            throw new InvalidOperationException("积分抵扣订单必须指定客户");
        if (amount <= 0)
            throw new InvalidOperationException("积分抵扣金额必须大于0");

        await _pointsDeductionService.DeductAsync(
            order.CustomerId.Value, order.TenantId, order.TenantCode,
            order.StoreId, order.StoreCode,
            amount, order.Id, order.OrderNo, "订单",
            order.OperatorId, now);

        // 仅单一积分支付（PayMethod=6）在此更新累计消费；组合支付（PayMethod=7）由 AwardPointsAsync 统一处理 CashAmount+StoredValueAmount
        if (order.PayMethod == 6)
        {
            var customer = await _dbContext.Customers
                .FirstOrDefaultAsync(c => c.Id == order.CustomerId.Value && c.TenantId == order.TenantId);
            if (customer != null)
            {
                customer.TotalConsume += amount;
                customer.LastConsumeTime = now;
                customer.UpdatedTime = now;
            }
        }
    }

    /// <summary>
    /// 积分发放：按积分规则 PointsRate 计算，Floor 取整
    /// 内置显式防护：OrderType=3 项目卡核销直接返回（购买时已发放）
    /// PayMethod=5/6 不发积分的判断已由 CreateAsync 调用方控制，本方法不再重复
    /// 生效规则查询与生日当天双倍逻辑由 IPointsRuleService 统一处理
    /// </summary>
    /// <param name="baseAmount">发放基数（单一支付为 PaidAmount；组合支付为 CashAmount+StoredValueAmount，排除积分抵扣部分）</param>
    private async Task AwardPointsAsync(OrderEntity order, decimal baseAmount, DateTime now)
    {
        if (!order.CustomerId.HasValue || baseAmount <= 0) return;

        // 显式防护：项目卡核销不发积分（购买时已发放），与 CreateAsync 调用处条件形成双重保险
        if (order.OrderType == 3) return;

        var pointsRule = await _pointsRuleService.GetEffectivePointsRuleAsync(order.TenantId, order.StoreId);
        if (pointsRule == null) return;

        // 先查客户（生日当天双倍判断需要客户生日）
        var customer = await _dbContext.Customers
            .FirstOrDefaultAsync(c => c.Id == order.CustomerId.Value && c.TenantId == order.TenantId);
        if (customer == null) return;

        // 计算应发积分（含生日当天双倍）
        var points = _pointsRuleService.CalculateAwardPoints(pointsRule, customer.Birthday, baseAmount, now);
        if (points <= 0) return;

        order.Points = points;
        // 记录下单时的抵扣比例快照，用于退款时折算积分不足部分（避免规则变更后计算错误）
        order.DeductRate = pointsRule.DeductRate;

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
    /// ProductType 按商品实际类型按行映射（实物 Type=1→1零售，服务 Type=2→2服务），
    /// 不再使用 order.OrderType 单值映射——混合订单（零售+服务合单，OrderType=2）中
    /// 零售商品须统计为 ProductType=1，服务商品统计为 ProductType=2（WP1 统计错乱修复）
    /// 赠品（Type=5）不计入销售统计，由 giftProductIds 跳过（V4 方案，P-SG-03）
    /// </summary>
    private async Task RecordProductSalesStatAsync(
        OrderEntity order, Dictionary<long, int> productTypeMap, HashSet<long> giftProductIds, DateTime now)
    {
        var statDate = order.OrderTime.Date;
        var statMonth = $"{statDate:yyyy-MM}";

        // 按商品聚合明细后逐一处理：同一商品多行明细（快速开单同服务选不同技师/时段会产生多行）先合并销量/金额，
        // 避免「查询-新建」模式在循环内对同商品重复 Add 统计行——EF 查询只读数据库、看不到内存中已 Add 未落库的行，
        // 会在 SaveChanges 时撞唯一索引 IX_ProductSalesStats_TenantId_StoreId_StatDate_ProductId（23505）
        // 聚合前已过滤赠品（Type=5 不计入销售统计）与不进入统计的类型（耗材/样品等，主档 Type 非 1/2）
        var aggregates = order.OrderItems
            .Where(i => !giftProductIds.Contains(i.ProductId)
                && productTypeMap.TryGetValue(i.ProductId, out var masterType)
                && (masterType == 1 || masterType == 2))
            .GroupBy(i => i.ProductId)
            .Select(g => new
            {
                g.Key,
                // 按商品实际类型判定统计类型：实物→1零售，服务→2服务（分组内同商品主档类型恒定）
                ProductType = productTypeMap[g.Key] == 1 ? 1 : 2,
                ProductName = g.First().ProductName,
                Quantity = g.Sum(i => i.Quantity),
                Amount = g.Sum(i => i.DiscountedAmount)
            })
            .ToList();

        foreach (var agg in aggregates)
        {
            // 并发加固：事务级顾问锁按 (租户, 门店, 统计日, 商品) 串行化统计写入，
            // 避免两台收银机同时结算含同一商品的订单时，双双走"新建"分支撞唯一索引（TOCTOU 竞态）
            // 锁随订单事务提交/回滚自动释放（本方法调用链位于订单事务内）
            var lockKey = BuildProductSalesStatLockKey(order.TenantId, order.StoreId, statDate, agg.Key);
            await _dbContext.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock({0})", lockKey);

            // 加锁后重新查询：后到事务在锁释放后能读到先到事务已提交的统计行，走累加而非新建
            var existing = await _dbContext.ProductSalesStats
                .FirstOrDefaultAsync(s => s.TenantId == order.TenantId
                    && s.StoreId == order.StoreId
                    && s.ProductId == agg.Key
                    && s.ProductType == agg.ProductType
                    && s.StatDate == statDate);

            if (existing != null)
            {
                // 累加
                existing.SalesCount += (int)Math.Round(agg.Quantity);
                existing.SalesAmount += agg.Amount;
                existing.UpdatedTime = now;
            }
            else
            {
                // 新建
                _dbContext.ProductSalesStats.Add(new ProductSalesStat
                {
                    StatDate = statDate,
                    StatMonth = statMonth,
                    ProductId = agg.Key,
                    ProductName = agg.ProductName,
                    ProductType = agg.ProductType,
                    SalesCount = (int)Math.Round(agg.Quantity),
                    SalesAmount = agg.Amount,
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
    /// 构造商品销售统计的事务级顾问锁键
    /// 按 (租户, 门店, 统计日, 商品) 维度串行化并发结算对同一统计行的写入
    /// 必须用确定性哈希：不能用 HashCode.Combine（其内部使用随机种子，跨调用/跨进程值不同会使锁形同虚设）
    /// </summary>
    private static long BuildProductSalesStatLockKey(long tenantId, long storeId, DateTime statDate, long productId)
    {
        var dateInt = int.Parse(statDate.ToString("yyyyMMdd"));
        unchecked
        {
            var key = tenantId;
            key = key * 31 + storeId;
            key = key * 31 + dateInt;
            key = key * 31 + productId;
            return key & long.MaxValue;
        }
    }

    /// <summary>
    /// 退款时扣减商品销售统计（按退款比例扣减 SalesCount/SalesAmount）
    /// 按订单下单日期查找对应统计记录
    /// ProductType 按商品实际类型逐行映射（与创建侧 RecordProductSalesStatAsync 对称），
    /// 混合订单（零售+服务合单）退款时实物/服务行各自回补对应 ProductType 的统计（WP1 对称修复）
    /// </summary>
    private async Task RefundProductSalesStatAsync(OrderEntity order, decimal ratio, List<string> actions, DateTime now)
    {
        var statDate = order.OrderTime.Date;
        var items = await _dbContext.OrderItems
            .Where(oi => oi.OrderId == order.Id)
            .ToListAsync();

        var productIds = items.Select(i => i.ProductId).Distinct().ToList();
        // 商品类型按主档 Type 取值：必须用 Select 表达式投影（EF 翻译为 JOIN，在数据库端取值）
        // 不能在 ToDictionaryAsync 的 Func 委托里访问 Master 导航——导航未 Include 加载时恒为 null，会抛空引用
        var productTypeMap = (await _dbContext.Products
                .Where(p => productIds.Contains(p.Id) && !p.IsDeleted)
                .Select(p => new { p.Id, MasterType = p.Master != null ? p.Master.Type : (int?)null })
                .ToListAsync())
            .ToDictionary(m => m.Id, m => m.MasterType!.Value);

        // 按商品聚合明细后逐一扣减：与创建侧 RecordProductSalesStatAsync 对称——同一商品多行明细（同服务选不同技师/时段产生多行）
        // 先合并数量/金额，避免循环内对同一统计行重复扣减（销售统计只记一条，退款按总量扣一次）
        var aggregates = items
            // 按商品实际类型判定统计类型：实物→1零售，服务→2服务；其余类型（赠品/耗材/样品等）不进入订单销售统计
            .Where(i => productTypeMap.TryGetValue(i.ProductId, out var masterType)
                && (masterType == 1 || masterType == 2))
            .GroupBy(i => i.ProductId)
            .Select(g => new
            {
                g.Key,
                // 分组内同商品主档类型恒定，取组首行映射即可
                ProductType = productTypeMap[g.Key] == 1 ? 1 : 2,
                Quantity = g.Sum(i => i.Quantity),
                Amount = g.Sum(i => i.DiscountedAmount)
            })
            .ToList();

        foreach (var agg in aggregates)
        {
            var stat = await _dbContext.ProductSalesStats
                .FirstOrDefaultAsync(s => s.TenantId == order.TenantId
                    && s.StoreId == order.StoreId
                    && s.ProductId == agg.Key
                    && s.ProductType == agg.ProductType
                    && s.StatDate == statDate);

            if (stat == null) continue;

            var deductCount = (int)Math.Round(agg.Quantity * ratio);
            var deductAmount = Math.Round(agg.Amount * ratio, 2);

            stat.SalesCount = Math.Max(0, stat.SalesCount - deductCount);
            stat.SalesAmount = Math.Max(0, stat.SalesAmount - deductAmount);
            stat.UpdatedTime = now;
        }

        actions.Add($"扣减商品销售统计（退款比例 {ratio:P1}）");
    }

    /// <summary>
    /// 取消订单（事务包裹，按 OrderType 全量回滚库存/BOM/项目卡/积分/储值/统计/消费记录）
    /// 订单 Status 改为 4（已取消），视为订单未发生
    /// 仅 Status=2(已完成) 的订单可取消；已退款(3)或已取消(4)的订单不可取消
    /// 取消原因记录在 RefundReason 字段（语义为"订单回滚原因"，由 Status 区分取消/退款）
    /// </summary>
    public async Task<ApiResponseDto> CancelAsync(long id, OrderCancelDto dto)
    {
        // 审计日志语义化：标记业务动作类型（区别于拦截器自动记录的 Update/Delete）
        _auditLogContext.CustomOperationType = "订单取消";

        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("登录状态异常，请重新登录", 401);

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

            // 仅已完成(2)的订单可取消；已退款(3)或已取消(4)的订单不可取消；
            // 存在退款金额（部分退款后 Status 仍为 2）的订单同样不可取消，避免重复回滚
            if (order.Status != 2)
                return ApiResponseDto.Fail("仅已完成的订单可取消", 400);
            if (order.RefundAmount > 0)
                return ApiResponseDto.Fail("订单已存在退款记录，不可取消", 400);

            // 更新订单状态为已取消，记录取消原因（复用 RefundReason 字段存储 reversal reason，由 Status 区分取消/退款）
            // 参照退款逻辑累加累计退款金额（全额）：前端实付金额/消费记录按 paidAmount - refundAmount 展示，
            // 累加后归零；跨日反日结时该订单 PaidAmount 与 RefundAmount 同日抵消，避免已取消订单虚增营收
            order.Status = 4;
            order.RefundAmount += order.PaidAmount;
            order.RefundTime = now;
            order.RefundReason = dto.Reason;
            order.UpdatedTime = now;

            // 全量回滚库存/BOM/项目卡/积分/储值/统计/消费记录
            await ReverseOrderAsync(order, actions, now);

            // 跨日取消订单触发原下单日反日结（退款/取消按原下单日归属，当日已汇总/确认的日结同样重算）
            // 对该日的 DailySettlement 反日结或补建，提示店主重新确认
            var reverseReason = $"跨日取消订单自动反日结：订单 {order.OrderNo}（实付 {order.PaidAmount:F2} 元）";
            var reversedDates = await ReverseSettlementForOrderReversalAsync(order, reverseReason, now);
            if (reversedDates.Any())
            {
                actions.Add($"订单下单日 {string.Join("、", reversedDates.Select(d => d.ToString("yyyy-MM-dd")))} 日结已自动反日结/补建，请前往日结管理重新确认");
            }

            // 技师统计回退：取消（Status→4）后订单不再满足归集条件，按明细涉及的 (技师, 服务日期) 重算剔除
            // 需先 SaveChanges 将 Status=4 落库，重算的 Status==2 过滤才看不到该订单（SQL 层查询，非内存跟踪）
            await _dbContext.SaveChangesAsync();
            await RecalculateTechnicianStatsForOrderAsync(order);

            await transaction.CommitAsync();

            // 取消可能涉及库存回库，即时检测低库存和积压预警（失败不影响取消结果，定时任务兜底）
            await CheckAlertsAfterInventoryChangeAsync(order.Id, order.TenantId, order.StoreId);

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
    /// 订单退款（事务包裹，按 OrderType 联动库存/项目卡/储值/积分）
    /// 支持部分退款：按 refundAmount / paidAmount 比例计算退库存和扣积分
    /// 全额退款时订单状态改为 3(已退款)
    /// OrderType=3 项目卡核销订单：PaidAmount=0，RefundAmount 必须为0，按全额回滚项目卡次数与库存/BOM
    /// </summary>
    public async Task<ApiResponseDto<RefundResultDto>> RefundAsync(RefundRequestDto dto)
    {
        // 审计日志语义化：标记业务动作类型
        _auditLogContext.CustomOperationType = "订单退款";

        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<RefundResultDto>.Fail("登录状态异常，请重新登录", 401);

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
            // OrderType=3 核销订单 PaidAmount=0，RefundAmount 必须为0（仅回退项目卡次数与库存/BOM，无款项退还）
            // OrderType=1/2 必须有实际退款金额（RefundAmount > 0）
            if (order.OrderType == 3 && dto.RefundAmount != 0)
                return ApiResponseDto<RefundResultDto>.Fail("项目卡核销订单无实际款项，退款金额必须为0", 400);
            if (order.OrderType != 3 && dto.RefundAmount <= 0)
                return ApiResponseDto<RefundResultDto>.Fail("退款金额必须大于0", 400);

            // 3.1 校验退款金额（不超过实付金额减去已退款金额，OrderType=3 跳过：PaidAmount=0）
            if (order.OrderType != 3)
            {
                var remainingRefundable = order.PaidAmount - order.RefundAmount;
                if (dto.RefundAmount > remainingRefundable)
                    return ApiResponseDto<RefundResultDto>.Fail($"退款金额超出可退金额（最多可退 {remainingRefundable:F2} 元）", 400);
            }

            // 退款比例（用于按比例扣积分与销售统计；库存回退已改为按门店选择的 RefundItems 精确驱动，不再按比例）
            // OrderType=3 PaidAmount=0，ratio 固定 1.0 便于全额扣减积分/统计
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

            // 库存回退规则（退款）：退库由门店在退款弹窗手动选择的 RefundItems 精确驱动
            // - 退库明细粒度 OrderItemBatch（商品 × 批次），只回退到原批次（BatchId），绝不新建退货批次；
            // - 服务项目/项目卡核销行的 BOM 耗材扣减明细同样在 RefundItems 中（OrderItemBatch.ProductId = 耗材）；
            // - 退库数量与退款金额相互独立：金额按实付比例联动（储值/积分/统计），退库按门店勾选精确执行；
            // - RefundItems 为空：本次退款不执行库存回退（仅退金额/次数等非库存联动）。
            // 4.1 解析并校验门店选择的退库明细（粒度 OrderItemBatch，只从订单已有批次中退回）
            var refundItems = new List<(OrderItemBatch batch, decimal qty)>();
            if (dto.RefundItems is { Count: > 0 })
            {
                var selectedIds = dto.RefundItems
                    .Where(r => r.Quantity > 0)
                    .Select(r => r.OrderItemBatchId)
                    .Distinct()
                    .ToList();
                var selectedBatches = await _dbContext.OrderItemBatches
                    .Where(oib => oib.OrderId == order.Id && selectedIds.Contains(oib.Id))
                    .ToDictionaryAsync(oib => oib.Id);
                foreach (var item in dto.RefundItems)
                {
                    if (item.Quantity <= 0) continue;
                    if (!selectedBatches.TryGetValue(item.OrderItemBatchId, out var batch))
                        return ApiResponseDto<RefundResultDto>.Fail($"退库明细批次(ID:{item.OrderItemBatchId})不属于该订单", 400);
                    var remaining = batch.Quantity - batch.RefundedQuantity;
                    if (item.Quantity > remaining)
                        return ApiResponseDto<RefundResultDto>.Fail($"退库数量超出可退数量：批次 {batch.BatchNo} 可退 {remaining:F4}", 400);
                    refundItems.Add((batch, item.Quantity));
                }
            }

            // 5. 按 OrderType 联动（库存回退规则见上方注释）
            switch (order.OrderType)
            {
                case 1: // 零售：按门店选择退实物商品批次
                case 2: // 服务：按门店选择退 BOM 耗材批次（OrderItemBatch.ProductId = 耗材）
                    await RefundInventoryBySelectedAsync(order, refundItems, actions, now, useLegacyWhenEmpty: false);
                    break;
                case 3: // 项目卡核销：回退次数（必做）+ 按门店选择退商品/BOM 耗材批次
                    await RefundTreatmentCardVerifyAsync(order, actions, now);
                    await RefundInventoryBySelectedAsync(order, refundItems, actions, now, useLegacyWhenEmpty: false);
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

            // 7.3 冲减客户累计消费（按退款比例，与取消订单 ReverseOrderAsync 对称）
            // 累计消费计入口径与创建侧一致：单一支付=PaidAmount，组合支付=CashAmount+StoredValueAmount（积分抵扣部分不计入）
            // 项目卡核销订单（OrderType=3）PaidAmount=0，consumeAmount=0 自动跳过，不误冲减
            if (order.CustomerId.HasValue)
            {
                var consumeAmount = order.PayMethod == 7
                    ? (order.CashAmount ?? 0m) + (order.StoredValueAmount ?? 0m)
                    : order.PaidAmount;
                if (consumeAmount > 0 && ratio > 0)
                {
                    var consumeCustomer = await _dbContext.Customers
                        .FirstOrDefaultAsync(c => c.Id == order.CustomerId.Value && c.TenantId == tenantId);
                    if (consumeCustomer != null)
                    {
                        consumeCustomer.TotalConsume = Math.Max(0m, consumeCustomer.TotalConsume - Math.Round(consumeAmount * ratio, 2));
                        consumeCustomer.UpdatedTime = now;
                        actions.Add($"客户 ID:{consumeCustomer.Id} 冲减累计消费 {Math.Round(consumeAmount * ratio, 2):F2}（退款比例 {ratio:P1}）");
                    }
                }
            }

            // 8. 扣减商品销售统计（ProductSalesStat，按退款比例）
            await RefundProductSalesStatAsync(order, ratio, actions, now);

            // 9. 退款触发原下单日反日结（退款按原下单日归属，当日已汇总/确认的日结同样重算）
            // 对该日的 DailySettlement 反日结或补建，提示店主重新确认
            var reverseReason = $"跨日退款自动反日结：订单 {order.OrderNo} 退款 {actualRefundAmount:F2} 元";
            var reversedDates = await ReverseSettlementForOrderReversalAsync(order, reverseReason, now);
            if (reversedDates.Any())
            {
                actions.Add($"订单下单日 {string.Join("、", reversedDates.Select(d => d.ToString("yyyy-MM-dd")))} 日结已自动反日结/补建，请前往日结管理重新确认");
            }

            await _dbContext.SaveChangesAsync();

            // 技师统计回退：全额退款（Status→3）后订单不再满足归集条件，按明细涉及的 (技师, 服务日期) 重算剔除
            // 部分退款仍 Status=2，重算结果无变化，跳过（部分退款按全额计入，业务已确认）
            if (order.Status == 3)
            {
                await RecalculateTechnicianStatsForOrderAsync(order);
            }

            await transaction.CommitAsync();

            // 退款可能涉及库存回库，即时检测低库存和积压预警（失败不影响退款结果，定时任务兜底）
            await CheckAlertsAfterInventoryChangeAsync(order.Id, order.TenantId, order.StoreId);

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
    /// 订单回滚（退款/取消）触发原下单日反日结或补建（退款/取消按原下单日归属冲减，见"日结数据汇总规则"）
    /// 对该日的 DailySettlement 执行：
    /// - 已确认(Status=1)：状态置 0，记录反日结原因，重新汇总字段（DailyStat 等店主重新确认时由 ConfirmAsync 同步）
    /// - 待确认(Status=0)：仅重新汇总字段（数据已是最新）
    /// - 不存在：跨日订单创建一条待确认记录（兜底任务本应覆盖 30 天内日期，未覆盖视为 bug，需补建等店主确认）；
    ///   当日订单不补建（店主日后手动汇总/兜底任务基于实时聚合取到最新数据）
    /// 当日已生成日结的订单（店主已提前汇总/确认当日日结后又发生退款/取消）与跨日同口径处理。
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

        var settlement = await _dbContext.DailySettlements
            .FirstOrDefaultAsync(s => s.TenantId == order.TenantId
                && s.StoreId == order.StoreId
                && s.SettlementDate == orderDate);

        // 当日（或未来日，理论上不应出现）订单：仅当当日日结已生成时才重算/反日结
        // 当日日结尚未生成时，店主日后手动汇总/兜底任务基于实时聚合（SummarizeCoreAsync）取到最新数据，无需在此处理；
        // 已生成（店主已提前汇总/确认）则与跨日同口径：重算字段，已确认则反日结提示重新确认
        if (settlement == null && orderDate >= DateTime.Today)
            return reversedDates;

        // 重新汇总该日数据（无论原状态，都更新字段，确保数据最新）
        // 前置保存挂起的变更：补录/退款/取消流程在本事务内刚写入的出库/回库流水尚在 EF ChangeTracker 中，
        // 而 SummarizeCoreAsync 的营收/成本/退款统计经 Sum/Join 聚合在数据库端执行，无法合并未落库实体，
        // 会导致"有营收无成本"（营收查已落库订单、成本查未落库流水）。先落库确保聚合统计到最新数据。
        await _dbContext.SaveChangesAsync();
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
    /// 订单全量回滚内部方法（取消订单使用）：按 OrderType 联动回滚库存/BOM/项目卡/积分/储值/统计/消费记录
    /// 调用方需已开启事务并加载订单实体。本方法不更新订单状态（由调用方设置 Status=4 已取消）
    /// 与 RefundAsync 的区别：
    /// - 全量回滚（ratio=1.0），不支持部分金额
    /// - 删除 ConsumeLog（取消订单视为未发生消费）
    /// - 回退 Customer.TotalConsume（按订单实付金额）
    /// 库存回退规则（取消订单）：整单作废，构建订单全部可退批次（RefundedQuantity &lt; Quantity）全量回退到原批次，
    /// 与退款（RefundAsync 按门店 RefundItems 选择）共用 RefundInventoryBySelectedAsync：
    /// 只回退到原批次（BatchId）、绝不新建退货批次。
    /// 注意：Customer.LastConsumeTime 不回退（无法准确还原历史值，且为非关键展示字段）
    /// </summary>
    private async Task ReverseOrderAsync(OrderEntity order, List<string> actions, DateTime now)
    {
        const decimal ratio = 1.0m;

        // 1. 按 OrderType 联动库存/BOM/项目卡（取消 = 整单作废，全量回退订单所有可退批次）
        // 取消订单无手动选择 UI，构建订单全部可退批次统一精确退库（useLegacyWhenEmpty=true 兜底无批次记录的历史订单）
        switch (order.OrderType)
        {
            case 1: // 零售：全量退实物商品批次
            case 2: // 服务：全量退 BOM 耗材批次（创建时已写入 OrderItemBatch）
                await RefundInventoryBySelectedAsync(order, await BuildAllRefundableBatchesAsync(order), actions, now, useLegacyWhenEmpty: true);
                break;
            case 3: // 项目卡核销：回退次数 + 全量退商品/BOM 耗材批次
                await RefundTreatmentCardVerifyAsync(order, actions, now);
                await RefundInventoryBySelectedAsync(order, await BuildAllRefundableBatchesAsync(order), actions, now, useLegacyWhenEmpty: true);
                break;
        }

        // 2. 积分扣减（按全额比例，OrderType=3 核销不发积分，order.Points=0 自动跳过）
        // 积分不足时差额按 DeductRate 折算现金从退款扣除，与退款 RefundAsync 保持一致（避免积分已使用导致超退）
        decimal pointsCashDeduction = 0;
        if (order.Points > 0)
        {
            pointsCashDeduction = await RefundPointsAsync(order, ratio, actions, now);
            if (pointsCashDeduction > 0)
            {
                order.RefundAmount -= pointsCashDeduction;
            }
        }

        // 3. 按 PayMethod 联动支付退款（全额扣除积分折算后的实际退款金额）
        // OrderType=3 核销订单 PaidAmount=0，以下条件自动跳过
        var actualRefundAmount = order.PaidAmount - pointsCashDeduction;
        if (order.PayMethod == 5 && actualRefundAmount > 0)
        {
            await RefundStoredValueAsync(order, actualRefundAmount, actions, now);
        }
        if (order.PayMethod == 6 && actualRefundAmount > 0)
        {
            await RefundPointsPaymentAsync(order, actualRefundAmount, actions, now);
        }
        if (order.PayMethod == 7 && actualRefundAmount > 0)
        {
            await RefundCombinedPaymentAsync(order, actualRefundAmount, actions, now);
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
    /// 按订单明细涉及的 (技师, 服务日期) 去重后逐一重算技师统计（归集/回退共用，天然幂等）
    /// 仅商家技师（TechnicianSource==1）参与归集；服务时间日期为空回退下单日
    /// 创建场景明细已在内存跟踪中可直接使用；退款/取消场景订单从库加载（未 Include 明细）则回查 OrderItems
    /// </summary>
    private async Task RecalculateTechnicianStatsForOrderAsync(OrderEntity order)
    {
        // 订单明细：内存中已加载（创建）则直接用，否则从数据库查询（退款/取消场景）
        List<OrderItem> orderItems;
        if (order.OrderItems != null && order.OrderItems.Any())
        {
            orderItems = order.OrderItems;
        }
        else
        {
            orderItems = await _dbContext.OrderItems
                .Where(oi => oi.OrderId == order.Id)
                .ToListAsync();
        }

        // 按 (技师, 服务日期) 去重，仅商家技师参与归集
        var keys = orderItems
            .Where(oi => oi.TechnicianId.HasValue && oi.TechnicianSource == 1)
            .Select(oi => new
            {
                TechnicianId = oi.TechnicianId!.Value,
                StatDate = (oi.ServiceStartTime ?? order.OrderTime).Date
            })
            .Distinct()
            .ToList();

        foreach (var key in keys)
        {
            await _technicianStatisticAppService.RecalculateTechnicianStatisticAsync(
                order.TenantId, order.StoreId, key.TechnicianId, key.StatDate);
        }
    }

    /// <summary>
    /// 退款/取消的库存回退统一执行：按传入的 OrderItemBatch（商品 × 批次）+ 数量精确回退。
    /// 库存回退规则（退款/取消一致）：
    /// - 只从订单中已有的批次退回，回退到原批次（OrderItemBatch.BatchId 关联的 InventoryBatch），绝不新建退货批次；
    /// - 原批次不存在（已删除/无 BatchId）时跳过并记录原因，不执行退库；
    /// - 同步更新 OrderItemBatch.RefundedQuantity（B5.5 效期销售统计按 Quantity - RefundedQuantity 计算实际销售）。
    /// 说明：
    /// - 退款（RefundAsync）：refundItems 来自门店在退款弹窗手动勾选的 RefundItems，退库数量与退款金额相互独立；
    /// - 取消（ReverseOrderAsync）：refundItems 为该订单全部可退批次（BuildAllRefundableBatchesAsync 构建），useLegacyWhenEmpty=true。
    /// </summary>
    private async Task RefundInventoryBySelectedAsync(
        OrderEntity order,
        IReadOnlyList<(OrderItemBatch batch, decimal qty)> refundItems,
        List<string> actions,
        DateTime now,
        bool useLegacyWhenEmpty)
    {
        if (!refundItems.Any())
        {
            if (useLegacyWhenEmpty)
            {
                // 取消场景：兼容无 OrderItemBatch 记录的历史订单，回退到原在库批次（不新建退货批次）
                await RefundRetailInventoryLegacyAsync(order, 1.0m, actions, now);
            }
            else
            {
                // 退款场景：门店未选择退库批次，本次退款不执行库存回退
                actions.Add("未选择退库批次，本次退款不执行库存回退");
            }
            return;
        }

        // 预加载原批次信息（按 BatchId 分组查询，减少数据库往返）
        var batchIds = refundItems.Where(x => x.batch.BatchId.HasValue).Select(x => x.batch.BatchId!.Value).Distinct().ToList();
        var originalBatches = await _dbContext.InventoryBatches
            .Where(b => batchIds.Contains(b.Id))
            .ToDictionaryAsync(b => b.Id);

        // 按 ProductId 分组更新 Inventory 汇总表（一次查询多个商品）
        var productIds = refundItems.Select(x => x.batch.ProductId).Distinct().ToList();
        var inventories = await _dbContext.Inventories
            .Where(i => productIds.Contains(i.ProductId) && i.TenantId == order.TenantId && i.StoreId == order.StoreId)
            .ToDictionaryAsync(i => i.ProductId);

        foreach (var (itemBatch, qty) in refundItems)
        {
            var refundQty = qty;
            if (refundQty <= 0) continue;

            // 回退到原批次：仅当原批次仍存在（BatchId 关联未删除），绝不新建退货批次
            // 原批次不存在（已删除/无 BatchId）时跳过退库并记录，避免凭空增加库存产生假退货流水
            if (!itemBatch.BatchId.HasValue || !originalBatches.TryGetValue(itemBatch.BatchId.Value, out var targetBatch))
            {
                actions.Add($"商品(ID:{itemBatch.ProductId}) 批次 {itemBatch.BatchNo} 原库存批次已不存在，跳过退库");
                continue;
            }

            targetBatch.Quantity += refundQty;
            targetBatch.UpdatedTime = now;
            // 若原批次已用完(Status=2)或已过期(Status=3)，恢复为在库
            if (targetBatch.Status != 1) targetBatch.Status = 1;

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
                // 无汇总记录时新建（极端情况：订单有批次扣减记录但汇总缺失，退款补齐）
                inventory = new Inventory
                {
                    ProductId = itemBatch.ProductId,
                    Quantity = refundQty,
                    TenantId = order.TenantId,
                    TenantCode = order.TenantCode,
                    StoreId = order.StoreId,
                    StoreCode = order.StoreCode,
                    CreatedTime = now
                };
                _dbContext.Inventories.Add(inventory);
                inventories[itemBatch.ProductId] = inventory;
            }

            // 记库存流水（Type=1入库, SourceType=退货入库，写回原批次号/效期用于效期统计）
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
                Remark = order.OrderNo,
                OperatorId = _currentUser.UserId,
                OperatorName = _currentUser.RealName ?? _currentUser.UserName,
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
    /// 构建订单全部可退批次（RefundedQuantity &lt; Quantity），用于取消订单全量回退
    /// </summary>
    private async Task<List<(OrderItemBatch batch, decimal qty)>> BuildAllRefundableBatchesAsync(OrderEntity order)
    {
        var itemBatches = await _dbContext.OrderItemBatches
            .Where(oib => oib.OrderId == order.Id && oib.RefundedQuantity < oib.Quantity)
            .ToListAsync();
        if (!itemBatches.Any())
            return new List<(OrderItemBatch, decimal)>();

        return itemBatches.Select(b => (b, b.Quantity - b.RefundedQuantity)).ToList();
    }

    /// <summary>
    /// 历史订单（无 OrderItemBatch 记录）取消时的库存回退兜底逻辑
    /// 仅回退到该商品已有的在库批次（Status=1，取最新创建），找不到在库批次则跳过并记录；
    /// 绝不新建退货批次（与主路径 RefundInventoryBySelectedAsync 的约束一致）
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

        // 商品主档类型映射：用于跳过服务项目（Type=2）——服务项目无实物库存，
        // 创建时未扣任何库存批次（无 BOM 时不写 OrderItemBatch），退库只会凭空增加库存并产生假退货流水
        var productIds = items.Select(i => i.ProductId).Distinct().ToList();
        var productTypeMap = (await _dbContext.Products
                .Where(p => productIds.Contains(p.Id) && !p.IsDeleted)
                .Select(p => new { p.Id, MasterType = p.Master != null ? p.Master.Type : (int?)null })
                .ToListAsync())
            .ToDictionary(m => m.Id, m => m.MasterType);

        foreach (var item in items)
        {
            // 服务项目（Type=2）跳过库存回退：其消耗的 BOM 耗材若已扣库存，
            // 由主路径 RefundInventoryBySelectedAsync 按 OrderItemBatch 精确退回；此处仅兜底无批次记录的历史订单
            if (productTypeMap.TryGetValue(item.ProductId, out var masterType) && masterType == 2)
            {
                actions.Add($"服务项目 {item.ProductName}(ID:{item.ProductId}) 跳过库存回退（无实物库存）");
                continue;
            }

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
                // 无在库批次可退：跳过退库并记录（绝不新建退货批次，避免凭空增加库存产生假退货流水）
                actions.Add($"商品 {item.ProductName}(ID:{item.ProductId}) 无在库批次可退，跳过库存回退（不新建退货批次）");
                continue;
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
                Remark = order.OrderNo,
                OperatorId = _currentUser.UserId,
                OperatorName = _currentUser.RealName ?? _currentUser.UserName,
                TenantId = order.TenantId,
                TenantCode = order.TenantCode,
                StoreId = order.StoreId,
                StoreCode = order.StoreCode,
                CreatedTime = now
            });

            actions.Add($"商品 {item.ProductName}(ID:{item.ProductId}) 退库存 {returnQty:F4}（历史订单兜底，回退到在库批次 {batch.BatchNo}）");
        }
    }

    /// <summary>
    /// 项目卡核销订单退款：通过 TreatmentCardVerify.OrderId 反查核销记录，回退 RemainingTimes 和 TotalConsumedAmount，已用完则恢复为有效
    /// 仅处理项目卡次数与金额的回退；库存/BOM 耗材回退由调用方（RefundAsync case 3 / ReverseOrderAsync）调用 RefundInventoryBySelectedAsync 统一处理
    /// （核销时按 Product.Type 联动扣减的库存/BOM 已写入 OrderItemBatch，可按批次精确退库）
    /// 与"核销冲正"接口（ReverseAsync）对称：同步标记核销记录 ReverseStatus=1 并冲减项目卡核销统计（ProductSalesStat ProductType=5），
    /// 避免核销订单退款后日结/看板/月度的项目卡核销折算营收与核销统计虚高（退款即撤销本次核销，营收应同步冲减）
    /// </summary>
    private async Task RefundTreatmentCardVerifyAsync(OrderEntity order, List<string> actions, DateTime now)
    {
        var verify = await _dbContext.TreatmentCardVerifies
            .Include(v => v.Items)
            .FirstOrDefaultAsync(v => v.OrderId == order.Id && v.TenantId == order.TenantId);
        if (verify == null)
        {
            actions.Add("未找到关联的项目卡核销记录，跳过项目卡回退");
            return;
        }

        var sale = await _dbContext.TreatmentCardSales
            .FirstOrDefaultAsync(s => s.Id == verify.CardSaleId && s.TenantId == order.TenantId && !s.IsDeleted);
        if (sale == null)
        {
            actions.Add("未找到项目卡销售记录，跳过项目卡回退");
            return;
        }

        sale.RemainingTimes += verify.VerifyTimes;
        sale.TotalConsumedAmount -= verify.VerifyAmount;
        sale.UpdatedTime = now;
        // 已用完的项目卡恢复为有效
        if (sale.Status == 2)
        {
            sale.Status = 1;
        }

        // 标记核销记录为已冲正（ReverseStatus=1）：日结项目卡核销折算营收按 ReverseStatus==0 过滤，
        // 若不标记，退款后该核销的 VerifyAmount 仍计入日结/看板/月度营收（权责发生制已撤销，营收应冲减）
        verify.ReverseStatus = 1;
        verify.UpdatedTime = now;

        // 冲减项目卡核销统计（ProductType=5），与核销创建（CreateAsync）及核销冲正（ReverseAsync）口径对称
        // 使用核销记录自身 StoreId（可能跨店核销），冲减原核销门店的业绩统计
        var statDate = verify.VerifyTime.Date;
        var productIds = verify.Items.Select(i => i.ProductId).Distinct().ToList();
        var stats = await _dbContext.ProductSalesStats
            .Where(s => s.TenantId == order.TenantId
                && s.StoreId == verify.StoreId
                && s.ProductType == 5
                && s.StatDate == statDate
                && productIds.Contains(s.ProductId))
            .ToListAsync();

        foreach (var stat in stats)
        {
            var item = verify.Items.FirstOrDefault(i => i.ProductId == stat.ProductId);
            if (item != null)
            {
                stat.SalesCount = Math.Max(0, stat.SalesCount - item.VerifyTimes);
                stat.SalesAmount = Math.Max(0m, stat.SalesAmount - item.SubAmount);
                stat.UpdatedTime = now;
            }
        }

        actions.Add($"项目卡销售记录 ID:{sale.Id} 回退次数 {verify.VerifyTimes}，回退金额 {verify.VerifyAmount:F2}；核销记录已冲正并冲减核销统计");
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
    /// 同步客户档案 Customer.Balance（口径为账户总余额，含赠送）
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

        // 同步客户档案余额：口径与储值账户总余额一致（实收+赠送）
        var customer = await _dbContext.Customers
            .FirstOrDefaultAsync(c => c.Id == order.CustomerId.Value && c.TenantId == order.TenantId);
        if (customer != null)
        {
            customer.Balance = account.Balance;
            customer.UpdatedTime = now;
        }

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
            OperatorId = _currentUser.UserId,
            OperatorName = _currentUser.RealName ?? _currentUser.UserName,
            TenantId = order.TenantId,
            TenantCode = order.TenantCode,
            StoreId = order.StoreId,
            StoreCode = order.StoreCode,
            CreatedTime = now
        });

        actions.Add($"储值账户 ID:{account.Id} 退余额 {refundAmount:F2}（仅实收余额）");
    }

    /// <summary>
    /// 积分抵扣订单退款：按订单 DeductRate 快照将退款金额换算为积分退还客户账户
    /// 当订单 PayMethod=6(积分抵扣)时，退款金额按比例换算为积分退还
    /// 使用订单快照而非当前规则，避免规则变更后退还积分数量不一致
    /// </summary>
    private async Task RefundPointsPaymentAsync(OrderEntity order, decimal refundAmount, List<string> actions, DateTime now)
    {
        if (!order.CustomerId.HasValue)
        {
            actions.Add("订单无客户ID，跳过积分退还");
            return;
        }

        // 使用订单 DeductRate 快照，避免规则变更后退还积分数量不一致
        var deductRate = order.DeductRate;
        if (deductRate <= 0)
        {
            actions.Add("订单未记录积分抵扣比例快照，跳过积分退还");
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
        var pointsToRefund = (int)Math.Floor(refundAmount / deductRate);
        if (pointsToRefund <= 0)
        {
            actions.Add($"退款金额 {refundAmount:F2} 不足1积分，跳过积分退还");
            return;
        }

        var beforePoints = customer.TotalPoints;
        customer.TotalPoints += pointsToRefund;
        customer.UpdatedTime = now;

        // 积分退还属于积分变动，必须记录积分流水（与 RefundPointsAsync/RefundStoredValueAsync 的流水规范保持一致）
        _dbContext.CustomerPointsLogs.Add(new CustomerPointsLog
        {
            CustomerId = customer.Id,
            Type = CustomerPointsLogType.RefundReturn, // 退款退还
            Points = pointsToRefund,
            BeforePoints = beforePoints,
            AfterPoints = customer.TotalPoints,
            OrderId = order.Id,
            OperatorId = _currentUser.UserId,
            Remark = $"订单 {order.OrderNo} 退款退还积分（积分抵扣 {refundAmount:F2}元 × 比例 1/{deductRate}）",
            TenantId = order.TenantId,
            TenantCode = order.TenantCode,
            StoreId = order.StoreId,
            StoreCode = order.StoreCode,
            CreatedTime = now
        });

        actions.Add($"客户 ID:{customer.Id} 退还积分 {pointsToRefund}（退款 {refundAmount:F2} × 比例 1/{deductRate}）");
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

        // 积分不足：扣减全部剩余积分，不足部分按订单 DeductRate 快照折算现金从退款扣除
        // 使用订单快照而非当前规则，避免规则变更后折现计算错误
        if (customer.TotalPoints + pointsToDeduct < 0)
        {
            var deductRate = order.DeductRate > 0 ? order.DeductRate : 0m;

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
