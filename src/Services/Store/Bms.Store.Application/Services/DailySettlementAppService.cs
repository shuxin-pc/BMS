using Mapster;
using Microsoft.EntityFrameworkCore;
using Bms.BuildingBlocks.Abstractions.Security;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.DailySettlements;
using Bms.Store.Domain.Entities;
using DailySettlementEntity = Bms.Store.Domain.Entities.DailySettlement;
using Bms.Store.Infrastructure;

namespace Bms.Store.Application.Services;

/// <summary>
/// 日结汇总聚合结果（供手动汇总、今日汇总、重算与后台兜底服务复用）
/// </summary>
public record SettlementSummaryData(
    decimal TotalRevenue,
    decimal TotalRefund,
    decimal TotalStoredValueRecharge,
    decimal TotalStoredValueConsume,
    int OrderCount,
    decimal TotalCost,
    decimal TotalGrossProfit,
    decimal CashRevenue,
    decimal StoredValueRevenue,
    decimal PointsDeductAmount,
    // 成本维度拆分（按 InventoryLog.SourceType 分类）
    decimal SalesOutboundCost,           // 销售出库成本（主营成本）
    decimal TreatmentCardOutboundCost,   // 疗程卡核销出库成本（主营成本）
    decimal InventoryLossAmount,         // 盘亏损失（营业外支出）
    decimal SampleGiftAmount,            // 样品赠品费用（营业外支出）
    decimal TransferOutAmount,           // 调拨出库金额（资产变动）
    decimal TransferInAmount,            // 调拨入库金额（资产变动）
    decimal PurchaseReturnAmount,        // 采购退货金额（资产变动）
    // 营收补充维度
    decimal TreatmentCardVerifyAmount,   // 疗程卡核销折算金额（权责发生制转营收）
    // 退款按 PayMethod 拆分（P-DS-03）
    decimal CashRefundAmount,            // 现金类退款（PayMethod=1-4 全额 + PayMethod=7 CashAmount 分摊，冲减营收）
    decimal StoredValueRefundAmount,     // 储值类退款（PayMethod=5 全额 + PayMethod=7 StoredValueAmount 分摊，不影响营收）
    decimal PointsRefundAmount);         // 积分类退款（PayMethod=6 全额 + PayMethod=7 PointsAmount 分摊，不影响营收）

/// <summary>
/// 日结管理应用服务实现
/// 日结两态：0 待确认 / 1 已确认；手动汇总为主入口，凌晨 02:00 后台兜底直接生成已确认记录
/// </summary>
public class DailySettlementAppService : IDailySettlementAppService
{
    private readonly StoreDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IMonthlyStatAppService _monthlyStatAppService;

    public DailySettlementAppService(
        StoreDbContext dbContext,
        ICurrentUser currentUser,
        IMonthlyStatAppService monthlyStatAppService)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _monthlyStatAppService = monthlyStatAppService;
    }

    /// <summary>
    /// 聚合指定门店指定日期的营业数据
    /// 无订单日返回全 0 数据（便于一眼看出汇总遗漏）
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <param name="storeId">门店ID</param>
    /// <param name="date">汇总日期（取日期部分）</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task<SettlementSummaryData> SummarizeCoreAsync(
        long tenantId, long storeId, DateTime date, CancellationToken cancellationToken = default)
    {
        var dateStart = date.Date;
        var dateEnd = dateStart.AddDays(1);

        // 订单：按下单时间落在当日，状态为已完成(2)或已退款(3)
        var dayOrders = _dbContext.Orders
            .Where(o => o.TenantId == tenantId && o.StoreId == storeId
                && o.OrderTime >= dateStart && o.OrderTime < dateEnd
                && (o.Status == 2 || o.Status == 3));

        // 营收聚合口径：当日下单的所有订单（含已退款），已退款订单的 PaidAmount 计入当日营收，
        // 通过 RefundAmount 在同日抵消。方案 A：退款追溯到原下单日，避免"当日下单当日退款"时退款无源之扣（P-DS-03 边界场景）
        var completedOrders = dayOrders;

        // 现金类营收：PayMethod=1-4 的 PaidAmount + PayMethod=7 的 CashAmount
        var cashRevenue = await completedOrders
            .SumAsync(o => (decimal?)(o.PayMethod >= 1 && o.PayMethod <= 4 ? o.PaidAmount
                : (o.PayMethod == 7 ? (o.CashAmount ?? 0m) : 0m)), cancellationToken) ?? 0m;

        // 储值扣款营收：PayMethod=5 的 PaidAmount + PayMethod=7 的 StoredValueAmount
        var storedValueRevenue = await completedOrders
            .SumAsync(o => (decimal?)(o.PayMethod == 5 ? o.PaidAmount
                : (o.PayMethod == 7 ? (o.StoredValueAmount ?? 0m) : 0m)), cancellationToken) ?? 0m;

        // 积分抵扣金额（仅记录，不纳入营收）：PayMethod=6 的 PaidAmount + PayMethod=7 的 PointsAmount
        var pointsDeductAmount = await completedOrders
            .SumAsync(o => (decimal?)(o.PayMethod == 6 ? o.PaidAmount
                : (o.PayMethod == 7 ? (o.PointsAmount ?? 0m) : 0m)), cancellationToken) ?? 0m;

        // 疗程卡核销折算金额：当日核销记录的 VerifyAmount 之和（权责发生制，核销转营收）
        var treatmentCardVerifyAmount = await _dbContext.TreatmentCardVerifies
            .Where(v => v.TenantId == tenantId && v.StoreId == storeId
                && v.VerifyTime >= dateStart && v.VerifyTime < dateEnd)
            .SumAsync(v => (decimal?)v.VerifyAmount, cancellationToken) ?? 0m;

        // 总营收 = 现金营收 + 储值营收 + 疗程卡核销折算 - 现金退款（储值消费已含在 PaidAmount 中，不重复计入）
        // 注：储值消费（StoredValueLog.Type=2）作为统计字段单独展示，不计入 Revenue 避免与 PaidAmount 重复
        var totalRevenue = cashRevenue + storedValueRevenue + treatmentCardVerifyAmount;

        // 订单数：已完成 + 已退款（按下单时间）
        var orderCount = await dayOrders.CountAsync(cancellationToken);

        // 退款按 PayMethod 拆分（P-DS-03）
        // 方案 A：按原下单日归属（而非退款发生日），当日下单且已退款的订单，RefundAmount 在下单日同日抵消营收
        // 注：过滤条件用 RefundAmount > 0 而非 Status == 3，确保部分退款（Status=2 但有退款金额）也计入
        // 现金类退款（冲减营收）：PayMethod=1-4 全额 + PayMethod=7 CashAmount 比例分摊
        // 储值类退款（不影响营收）：PayMethod=5 全额 + PayMethod=7 StoredValueAmount 比例分摊
        // 积分类退款（不影响营收）：PayMethod=6 全额 + PayMethod=7 PointsAmount 比例分摊
        var refundOrders = dayOrders.Where(o => o.RefundAmount > 0);

        var cashRefundAmount = await refundOrders
            .SumAsync(o => (decimal?)(
                (o.PayMethod >= 1 && o.PayMethod <= 4) ? o.RefundAmount
                : (o.PayMethod == 7 && o.PaidAmount > 0)
                    ? o.RefundAmount * ((o.CashAmount ?? 0m) / o.PaidAmount)
                    : 0m), cancellationToken) ?? 0m;

        var storedValueRefundAmount = await refundOrders
            .SumAsync(o => (decimal?)(
                (o.PayMethod == 5) ? o.RefundAmount
                : (o.PayMethod == 7 && o.PaidAmount > 0)
                    ? o.RefundAmount * ((o.StoredValueAmount ?? 0m) / o.PaidAmount)
                    : 0m), cancellationToken) ?? 0m;

        var pointsRefundAmount = await refundOrders
            .SumAsync(o => (decimal?)(
                (o.PayMethod == 6) ? o.RefundAmount
                : (o.PayMethod == 7 && o.PaidAmount > 0)
                    ? o.RefundAmount * ((o.PointsAmount ?? 0m) / o.PaidAmount)
                    : 0m), cancellationToken) ?? 0m;

        // 现金退款冲减营收
        totalRevenue -= cashRefundAmount;

        // 总退款（展示用）= 现金退款 + 储值退款 + 积分退款
        var totalRefund = cashRefundAmount + storedValueRefundAmount + pointsRefundAmount;

        // 储值流水：按创建时间落在当日
        var dayValueLogs = _dbContext.StoredValueLogs
            .Where(v => v.TenantId == tenantId && v.StoreId == storeId
                && v.CreatedTime >= dateStart && v.CreatedTime < dateEnd);

        // 储值充值：类型 1，实收金额
        var totalStoredValueRecharge = await dayValueLogs
            .Where(v => v.Type == 1)
            .SumAsync(v => (decimal?)v.RealAmount, cancellationToken) ?? 0m;

        // 储值消费：类型 2，Amount 为负数，取负得正
        var consumeSum = await dayValueLogs
            .Where(v => v.Type == 2)
            .SumAsync(v => (decimal?)v.Amount, cancellationToken) ?? 0m;
        var totalStoredValueConsume = -consumeSum;

        // 成本维度拆分：按 InventoryLog.SourceType 分组统计
        // 主营成本 = 销售出库（SalesOutbound）+ 疗程卡核销出库（TreatmentCardOutbound）
        // 营业外支出 = 盘亏损失（CheckAdjustment 出库）+ 样品赠品费用（SampleReceiveOutbound/GiftOutbound，兼容历史值 SampleGiftOutbound）
        // 资产变动 = 调拨出库（TransferOutbound）- 调拨入库（TransferInbound）+ 采购退货（PurchaseReturnOutbound）
        var outboundLogs = _dbContext.InventoryLogs
            .Where(l => l.TenantId == tenantId && l.StoreId == storeId
                && l.Type == 2
                && l.CreatedTime >= dateStart && l.CreatedTime < dateEnd);

        var salesOutboundCost = await outboundLogs
            .Where(l => l.SourceType == InventoryLogSourceTypes.SalesOutbound)
            .SumAsync(l => (decimal?)(Math.Abs(l.Quantity) * l.UnitPrice), cancellationToken) ?? 0m;

        var treatmentCardOutboundCost = await outboundLogs
            .Where(l => l.SourceType == InventoryLogSourceTypes.TreatmentCardOutbound)
            .SumAsync(l => (decimal?)(Math.Abs(l.Quantity) * l.UnitPrice), cancellationToken) ?? 0m;

        var inventoryLossAmount = await outboundLogs
            .Where(l => l.SourceType == InventoryLogSourceTypes.CheckAdjustment)
            .SumAsync(l => (decimal?)(Math.Abs(l.Quantity) * l.UnitPrice), cancellationToken) ?? 0m;

        // P-SG-02：样品赠品费用聚合三种 SourceType（兼容历史数据 SampleGiftOutbound=9 + 新细分 SampleReceiveOutbound=10 + GiftOutbound=11）
        // 历史数据迁移回填前，三种 SourceType 均可能出现，需全量统计避免遗漏
        var sampleGiftAmount = await outboundLogs
            .Where(l => l.SourceType == InventoryLogSourceTypes.SampleGiftOutbound
                     || l.SourceType == InventoryLogSourceTypes.SampleReceiveOutbound
                     || l.SourceType == InventoryLogSourceTypes.GiftOutbound)
            .SumAsync(l => (decimal?)(Math.Abs(l.Quantity) * l.UnitPrice), cancellationToken) ?? 0m;

        var transferOutAmount = await outboundLogs
            .Where(l => l.SourceType == InventoryLogSourceTypes.TransferOutbound)
            .SumAsync(l => (decimal?)(Math.Abs(l.Quantity) * l.UnitPrice), cancellationToken) ?? 0m;

        var purchaseReturnAmount = await outboundLogs
            .Where(l => l.SourceType == InventoryLogSourceTypes.PurchaseReturnOutbound)
            .SumAsync(l => (decimal?)(Math.Abs(l.Quantity) * l.UnitPrice), cancellationToken) ?? 0m;

        // 调拨入库（Type=1, SourceType=TransferInbound）
        var transferInAmount = await _dbContext.InventoryLogs
            .Where(l => l.TenantId == tenantId && l.StoreId == storeId
                && l.Type == 1
                && l.SourceType == InventoryLogSourceTypes.TransferInbound
                && l.CreatedTime >= dateStart && l.CreatedTime < dateEnd)
            .SumAsync(l => (decimal?)(Math.Abs(l.Quantity) * l.UnitPrice), cancellationToken) ?? 0m;

        // 主营成本 = 销售出库 + 疗程卡核销出库
        var totalCost = salesOutboundCost + treatmentCardOutboundCost;

        return new SettlementSummaryData(
            totalRevenue, totalRefund, totalStoredValueRecharge,
            totalStoredValueConsume, orderCount, totalCost, totalRevenue - totalCost,
            cashRevenue, storedValueRevenue, pointsDeductAmount,
            salesOutboundCost, treatmentCardOutboundCost,
            inventoryLossAmount, sampleGiftAmount,
            transferOutAmount, transferInAmount, purchaseReturnAmount,
            treatmentCardVerifyAmount,
            cashRefundAmount, storedValueRefundAmount, pointsRefundAmount);
    }

    /// <summary>
    /// 生成或更新指定日期的 DailyStat 记录（日结确认/兜底任务调用）
    /// 复用 SummarizeCoreAsync 获取基础数据，额外查询成本/客户/预约/预警指标
    /// </summary>
    /// <param name="tenantCode">租户编码（BackgroundService 无 HttpContext 时需传入，否则从 ICurrentUser 获取）</param>
    public async Task EnsureDailyStatAsync(long tenantId, long storeId, DateTime date, string? tenantCode = null)
    {
        var dateStart = date.Date;
        var dateEnd = dateStart.AddDays(1);
        var now = DateTime.Now;

        var data = await SummarizeCoreAsync(tenantId, storeId, date);

        // 消费客户数（去重）
        var consumeCustomerCount = await _dbContext.Orders
            .Where(o => o.TenantId == tenantId && o.StoreId == storeId
                && o.OrderTime >= dateStart && o.OrderTime < dateEnd
                && (o.Status == 2 || o.Status == 3)
                && o.CustomerId.HasValue)
            .Select(o => o.CustomerId.Value)
            .Distinct()
            .CountAsync();

        // 新客数
        var newCustomerCount = await _dbContext.Customers
            .Where(c => c.TenantId == tenantId
                && c.CreatedTime >= dateStart && c.CreatedTime < dateEnd)
            .CountAsync();

        // 预约数
        var appointmentCount = await _dbContext.Appointments
            .Where(a => a.TenantId == tenantId
                && a.AppointmentDate == dateStart)
            .CountAsync();

        // 库存预警数（未处理）
        var inventoryAlertCount = await _dbContext.InventoryAlerts
            .Where(a => a.TenantId == tenantId && a.StoreId == storeId && !a.IsProcessed)
            .CountAsync();

        // 查找是否已存在该日期的 DailyStat
        var existing = await _dbContext.DailyStats
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.StoreId == storeId && s.StatDate == dateStart);

        if (existing != null)
        {
            existing.Revenue = data.TotalRevenue;
            existing.CashRevenue = data.CashRevenue;
            existing.StoredValueRevenue = data.StoredValueRevenue;
            existing.PointsDeductAmount = data.PointsDeductAmount;
            existing.Cost = data.TotalCost;
            existing.SalesOutboundCost = data.SalesOutboundCost;
            existing.TreatmentCardOutboundCost = data.TreatmentCardOutboundCost;
            existing.InventoryLossAmount = data.InventoryLossAmount;
            existing.SampleGiftAmount = data.SampleGiftAmount;
            existing.TransferOutAmount = data.TransferOutAmount;
            existing.TransferInAmount = data.TransferInAmount;
            existing.PurchaseReturnAmount = data.PurchaseReturnAmount;
            existing.GrossProfit = data.TotalGrossProfit;
            existing.OrderCount = data.OrderCount;
            existing.RefundAmount = data.TotalRefund;
            existing.CashRefundAmount = data.CashRefundAmount;
            existing.StoredValueRecharge = data.TotalStoredValueRecharge;
            existing.StoredValueConsume = data.TotalStoredValueConsume;
            existing.ConsumeCustomerCount = consumeCustomerCount;
            existing.NewCustomerCount = newCustomerCount;
            existing.AppointmentCount = appointmentCount;
            existing.InventoryAlertCount = inventoryAlertCount;
            existing.TreatmentCardVerifyAmount = data.TreatmentCardVerifyAmount;
            existing.UpdatedTime = now;
        }
        else
        {
            var storeCode = await GetStoreCodeAsync(tenantId, storeId);
            _dbContext.DailyStats.Add(new DailyStat
            {
                StatDate = dateStart,
                Revenue = data.TotalRevenue,
                CashRevenue = data.CashRevenue,
                StoredValueRevenue = data.StoredValueRevenue,
                PointsDeductAmount = data.PointsDeductAmount,
                Cost = data.TotalCost,
                SalesOutboundCost = data.SalesOutboundCost,
                TreatmentCardOutboundCost = data.TreatmentCardOutboundCost,
                InventoryLossAmount = data.InventoryLossAmount,
                SampleGiftAmount = data.SampleGiftAmount,
                TransferOutAmount = data.TransferOutAmount,
                TransferInAmount = data.TransferInAmount,
                PurchaseReturnAmount = data.PurchaseReturnAmount,
                GrossProfit = data.TotalGrossProfit,
                OrderCount = data.OrderCount,
                RefundAmount = data.TotalRefund,
                CashRefundAmount = data.CashRefundAmount,
                StoredValueRecharge = data.TotalStoredValueRecharge,
                StoredValueConsume = data.TotalStoredValueConsume,
                ConsumeCustomerCount = consumeCustomerCount,
                NewCustomerCount = newCustomerCount,
                AppointmentCount = appointmentCount,
                InventoryAlertCount = inventoryAlertCount,
                TreatmentCardVerifyAmount = data.TreatmentCardVerifyAmount,
                TenantId = tenantId,
                TenantCode = tenantCode ?? _currentUser.TenantCode ?? string.Empty,
                StoreId = storeId,
                StoreCode = storeCode,
                CreatedTime = now
            });
        }
    }

    /// <summary>
    /// 获取今日经营汇总（实时聚合，不落库）
    /// </summary>
    public async Task<ApiResponseDto<TodaySummaryDto>> GetTodaySummaryAsync()
    {
        var ctx = ResolveTenantStore();
        if (ctx.Error != null)
            return ApiResponseDto<TodaySummaryDto>.Fail(ctx.Error, ctx.Code);

        var today = DateTime.Today;
        var data = await SummarizeCoreAsync(ctx.TenantId, ctx.StoreId, today);

        // 检查今日是否已存在日结记录（待确认或已确认）
        var existing = await _dbContext.DailySettlements
            .Where(s => s.TenantId == ctx.TenantId && s.StoreId == ctx.StoreId && s.SettlementDate == today)
            .Select(s => new { s.Id, s.Status })
            .FirstOrDefaultAsync();

        var dto = new TodaySummaryDto
        {
            Date = today,
            TotalRevenue = data.TotalRevenue,
            TotalRefund = data.TotalRefund,
            TotalStoredValueRecharge = data.TotalStoredValueRecharge,
            TotalStoredValueConsume = data.TotalStoredValueConsume,
            OrderCount = data.OrderCount,
            IsSettled = existing != null,
            SettlementId = existing?.Id
        };
        return ApiResponseDto<TodaySummaryDto>.Ok(dto);
    }

    /// <summary>
    /// 手动汇总日结（创建待确认记录）
    /// </summary>
    public async Task<ApiResponseDto<DailySettlementDto>> SummarizeAsync(ManualSummarizeRequestDto request)
    {
        var ctx = ResolveTenantStore();
        if (ctx.Error != null)
            return ApiResponseDto<DailySettlementDto>.Fail(ctx.Error, ctx.Code);

        var date = request?.Date?.Date ?? DateTime.Today;

        // 重复检查：该日期已有记录则阻止
        var exists = await _dbContext.DailySettlements
            .AnyAsync(s => s.TenantId == ctx.TenantId && s.StoreId == ctx.StoreId && s.SettlementDate == date);
        if (exists)
            return ApiResponseDto<DailySettlementDto>.Fail("该日期已存在日结记录", 400);

        var storeCode = await GetStoreCodeAsync(ctx.TenantId, ctx.StoreId);
        var data = await SummarizeCoreAsync(ctx.TenantId, ctx.StoreId, date);

        var entity = new DailySettlementEntity
        {
            TenantId = ctx.TenantId,
            TenantCode = _currentUser.TenantCode ?? string.Empty,
            StoreId = ctx.StoreId,
            StoreCode = storeCode,
            SettlementDate = date,
            SettlementTime = DateTime.Now,
            OperatorId = _currentUser.UserId,
            TotalRevenue = data.TotalRevenue,
            CashRevenue = data.CashRevenue,
            StoredValueRevenue = data.StoredValueRevenue,
            PointsDeductAmount = data.PointsDeductAmount,
            TotalRefund = data.TotalRefund,
            CashRefundAmount = data.CashRefundAmount,
            TreatmentCardVerifyAmount = data.TreatmentCardVerifyAmount,
            TotalStoredValueRecharge = data.TotalStoredValueRecharge,
            TotalStoredValueConsume = data.TotalStoredValueConsume,
            OrderCount = data.OrderCount,
            TotalCost = data.TotalCost,
            SalesOutboundCost = data.SalesOutboundCost,
            TreatmentCardOutboundCost = data.TreatmentCardOutboundCost,
            InventoryLossAmount = data.InventoryLossAmount,
            SampleGiftAmount = data.SampleGiftAmount,
            TransferOutAmount = data.TransferOutAmount,
            TransferInAmount = data.TransferInAmount,
            PurchaseReturnAmount = data.PurchaseReturnAmount,
            TotalGrossProfit = data.TotalGrossProfit,
            Status = 0,
            Source = 1, // 手动汇总
            Remark = request?.Remark,
            CreatedTime = DateTime.Now
        };

        _dbContext.DailySettlements.Add(entity);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto<DailySettlementDto>.Ok(entity.Adapt<DailySettlementDto>(), "汇总成功，待确认");
    }

    /// <summary>
    /// 确认日结（待确认 -> 已确认）
    /// </summary>
    public async Task<ApiResponseDto<DailySettlementDto>> ConfirmAsync(long id)
    {
        var ctx = ResolveTenantStore();
        if (ctx.Error != null)
            return ApiResponseDto<DailySettlementDto>.Fail(ctx.Error, ctx.Code);

        var entity = await GetOwnedAsync(ctx, id);
        if (entity == null)
            return ApiResponseDto<DailySettlementDto>.Fail("日结记录不存在", 404);

        if (entity.Status != 0)
            return ApiResponseDto<DailySettlementDto>.Fail("该记录非待确认状态，无法确认", 400);

        // 执行防漏单校验：canConfirm=false 阻止，警告不阻止
        var validation = await ValidateBeforeConfirmAsync(id);
        if (!validation.Data?.CanConfirm ?? false)
            return ApiResponseDto<DailySettlementDto>.Fail(validation.Data?.Warnings.FirstOrDefault() ?? "校验未通过，无法确认", 400);

        entity.Status = 1;
        entity.ConfirmedBy = _currentUser.UserId;
        entity.ConfirmedTime = DateTime.Now;
        entity.UpdatedTime = DateTime.Now;

        // 日结确认时同步生成/更新 DailyStat 记录
        await EnsureDailyStatAsync(ctx.TenantId, ctx.StoreId, entity.SettlementDate);

        await _dbContext.SaveChangesAsync();

        // 日结确认后触发当月月度统计聚合，确保月度数据实时同步（P-STAT-01）
        try
        {
            var tenantCode = _currentUser.TenantCode ?? string.Empty;
            await _monthlyStatAppService.AggregateFromDailyInternalAsync(
                ctx.TenantId, ctx.StoreId, tenantCode,
                entity.SettlementDate.Year, entity.SettlementDate.Month);
        }
        catch (Exception)
        {
            // 月度聚合失败不影响日结确认主流程，BackgroundService 会在次月 1 日兜底
        }

        return ApiResponseDto<DailySettlementDto>.Ok(entity.Adapt<DailySettlementDto>(), "日结已确认");
    }

    /// <summary>
    /// 反日结（已确认 -> 待确认），允许跳过中间日期
    /// </summary>
    public async Task<ApiResponseDto<DailySettlementDto>> ReverseAsync(long id, ReverseRequestDto request)
    {
        var ctx = ResolveTenantStore();
        if (ctx.Error != null)
            return ApiResponseDto<DailySettlementDto>.Fail(ctx.Error, ctx.Code);

        var entity = await GetOwnedAsync(ctx, id);
        if (entity == null)
            return ApiResponseDto<DailySettlementDto>.Fail("日结记录不存在", 404);

        if (entity.Status != 1)
            return ApiResponseDto<DailySettlementDto>.Fail("该记录非已确认状态，无法反日结", 400);

        entity.Status = 0;
        entity.ReversedBy = _currentUser.UserId;
        entity.ReversedTime = DateTime.Now;
        entity.ReversedReason = request?.Reason;
        entity.ConfirmedBy = null;
        entity.ConfirmedTime = null;
        entity.UpdatedTime = DateTime.Now;

        await _dbContext.SaveChangesAsync();
        return ApiResponseDto<DailySettlementDto>.Ok(entity.Adapt<DailySettlementDto>(), "已反日结，可重新确认或重算");
    }

    /// <summary>
    /// 重新汇总（仅待确认状态可重算，已确认需先反日结）
    /// </summary>
    public async Task<ApiResponseDto<DailySettlementDto>> RecalculateAsync(long id)
    {
        var ctx = ResolveTenantStore();
        if (ctx.Error != null)
            return ApiResponseDto<DailySettlementDto>.Fail(ctx.Error, ctx.Code);

        var entity = await GetOwnedAsync(ctx, id);
        if (entity == null)
            return ApiResponseDto<DailySettlementDto>.Fail("日结记录不存在", 404);

        if (entity.Status != 0)
            return ApiResponseDto<DailySettlementDto>.Fail("已确认记录不可重算，请先反日结", 400);

        var data = await SummarizeCoreAsync(ctx.TenantId, ctx.StoreId, entity.SettlementDate);
        entity.TotalRevenue = data.TotalRevenue;
        entity.CashRevenue = data.CashRevenue;
        entity.StoredValueRevenue = data.StoredValueRevenue;
        entity.PointsDeductAmount = data.PointsDeductAmount;
        entity.TotalRefund = data.TotalRefund;
        entity.CashRefundAmount = data.CashRefundAmount;
        entity.TreatmentCardVerifyAmount = data.TreatmentCardVerifyAmount;
        entity.TotalStoredValueRecharge = data.TotalStoredValueRecharge;
        entity.TotalStoredValueConsume = data.TotalStoredValueConsume;
        entity.OrderCount = data.OrderCount;
        entity.TotalCost = data.TotalCost;
        entity.SalesOutboundCost = data.SalesOutboundCost;
        entity.TreatmentCardOutboundCost = data.TreatmentCardOutboundCost;
        entity.InventoryLossAmount = data.InventoryLossAmount;
        entity.SampleGiftAmount = data.SampleGiftAmount;
        entity.TransferOutAmount = data.TransferOutAmount;
        entity.TransferInAmount = data.TransferInAmount;
        entity.PurchaseReturnAmount = data.PurchaseReturnAmount;
        entity.TotalGrossProfit = data.TotalGrossProfit;
        entity.SettlementTime = DateTime.Now;
        entity.OperatorId = _currentUser.UserId;
        entity.Source = 1; // 手动重算视为手动汇总
        entity.UpdatedTime = DateTime.Now;

        await _dbContext.SaveChangesAsync();
        return ApiResponseDto<DailySettlementDto>.Ok(entity.Adapt<DailySettlementDto>(), "已重新汇总");
    }

    /// <summary>
    /// 批量补日结（P-DS-07）
    /// 传入起止日期范围，循环为每个缺失日期创建日结记录，支持"仅创建待确认"和"创建并自动确认"两种模式
    /// 单次最多 90 天，避免事务过大
    /// </summary>
    public async Task<ApiResponseDto<BatchSummarizeResultDto>> BatchSummarizeAsync(BatchSummarizeRequestDto request)
    {
        var ctx = ResolveTenantStore();
        if (ctx.Error != null)
            return ApiResponseDto<BatchSummarizeResultDto>.Fail(ctx.Error, ctx.Code);

        if (request == null)
            return ApiResponseDto<BatchSummarizeResultDto>.Fail("请求参数不能为空", 400);

        if (request.StartDate.Date > request.EndDate.Date)
            return ApiResponseDto<BatchSummarizeResultDto>.Fail("起始日期不能晚于结束日期", 400);

        var totalDays = (request.EndDate.Date - request.StartDate.Date).Days + 1;
        if (totalDays > 90)
            return ApiResponseDto<BatchSummarizeResultDto>.Fail("单次批量最多 90 天", 400);

        if (request.EndDate.Date >= DateTime.Today)
            return ApiResponseDto<BatchSummarizeResultDto>.Fail("结束日期必须早于今天", 400);

        var result = new BatchSummarizeResultDto();
        for (var date = request.StartDate.Date; date <= request.EndDate.Date; date = date.AddDays(1))
        {
            // 检查是否已存在
            var exists = await _dbContext.DailySettlements
                .AnyAsync(s => s.TenantId == ctx.TenantId && s.StoreId == ctx.StoreId && s.SettlementDate == date);
            if (exists)
            {
                result.Skipped.Add(date);
                continue;
            }

            // 调用单日汇总（复用 SummarizeAsync 内部逻辑，但避免重复解析 ctx）
            var singleRequest = new ManualSummarizeRequestDto { Date = date, Remark = request.Remark };
            var singleResult = await SummarizeAsync(singleRequest);
            if (!singleResult.IsSuccess || singleResult.Data == null)
            {
                result.Failed.Add(new BatchSummarizeFailedItem { Date = date, Reason = singleResult.Message ?? "汇总失败" });
                continue;
            }

            // 自动确认
            if (request.AutoConfirm)
            {
                var confirmResult = await ConfirmAsync(singleResult.Data.Id);
                if (!confirmResult.IsSuccess)
                {
                    result.Failed.Add(new BatchSummarizeFailedItem
                    {
                        Date = date,
                        Reason = $"汇总成功但确认失败：{confirmResult.Message}"
                    });
                    continue;
                }
            }
            result.Success.Add(date);
        }

        return ApiResponseDto<BatchSummarizeResultDto>.Ok(result,
            $"批量补日结完成：成功 {result.Success.Count}，跳过 {result.Skipped.Count}，失败 {result.Failed.Count}");
    }

    /// <summary>
    /// 历史漏日结回补（P-DS-09）
    /// 管理员手动触发，跨度 <= 90 天，endDate 必须早于今天
    /// 用于一次性补全历史漏日结，避免逐日手动汇总
    /// </summary>
    public async Task<ApiResponseDto<BackfillResultDto>> BackfillAsync(DateTime startDate, DateTime endDate)
    {
        var ctx = ResolveTenantStore();
        if (ctx.Error != null)
            return ApiResponseDto<BackfillResultDto>.Fail(ctx.Error, ctx.Code);

        if (startDate.Date > endDate.Date)
            return ApiResponseDto<BackfillResultDto>.Fail("起始日期不能晚于结束日期", 400);

        if (endDate.Date >= DateTime.Today)
            return ApiResponseDto<BackfillResultDto>.Fail("结束日期必须早于今天", 400);

        var totalDays = (endDate.Date - startDate.Date).Days + 1;
        if (totalDays > 90)
            return ApiResponseDto<BackfillResultDto>.Fail("单次回补最多 90 天", 400);

        var storeCode = await GetStoreCodeAsync(ctx.TenantId, ctx.StoreId);
        var result = new BackfillResultDto { TotalDays = totalDays };
        var batchSize = 0;

        for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
        {
            var exists = await _dbContext.DailySettlements
                .AnyAsync(s => s.TenantId == ctx.TenantId && s.StoreId == ctx.StoreId && s.SettlementDate == date);
            if (exists)
            {
                result.SkippedCount++;
                continue;
            }

            var data = await SummarizeCoreAsync(ctx.TenantId, ctx.StoreId, date);
            var entity = new DailySettlementEntity
            {
                TenantId = ctx.TenantId,
                TenantCode = _currentUser.TenantCode ?? string.Empty,
                StoreId = ctx.StoreId,
                StoreCode = storeCode,
                SettlementDate = date,
                SettlementTime = DateTime.Now,
                OperatorId = _currentUser.UserId,
                TotalRevenue = data.TotalRevenue,
                CashRevenue = data.CashRevenue,
                StoredValueRevenue = data.StoredValueRevenue,
                PointsDeductAmount = data.PointsDeductAmount,
                TotalRefund = data.TotalRefund,
                CashRefundAmount = data.CashRefundAmount,
                TreatmentCardVerifyAmount = data.TreatmentCardVerifyAmount,
                TotalStoredValueRecharge = data.TotalStoredValueRecharge,
                TotalStoredValueConsume = data.TotalStoredValueConsume,
                OrderCount = data.OrderCount,
                TotalCost = data.TotalCost,
                SalesOutboundCost = data.SalesOutboundCost,
                TreatmentCardOutboundCost = data.TreatmentCardOutboundCost,
                InventoryLossAmount = data.InventoryLossAmount,
                SampleGiftAmount = data.SampleGiftAmount,
                TransferOutAmount = data.TransferOutAmount,
                TransferInAmount = data.TransferInAmount,
                PurchaseReturnAmount = data.PurchaseReturnAmount,
                TotalGrossProfit = data.TotalGrossProfit,
                Status = 1, // 回补直接生成已确认，可反日结修正
                Source = 1, // 手动触发视为手动汇总
                Remark = "历史漏日结回补",
                CreatedTime = DateTime.Now
            };
            _dbContext.DailySettlements.Add(entity);

            // 同步生成 DailyStat
            await EnsureDailyStatAsync(ctx.TenantId, ctx.StoreId, date);

            result.CreatedCount++;
            batchSize++;

            // 每 100 条提交一次，避免单次事务过大
            if (batchSize >= 100)
            {
                await _dbContext.SaveChangesAsync();
                batchSize = 0;
            }
        }

        if (batchSize > 0)
            await _dbContext.SaveChangesAsync();

        return ApiResponseDto<BackfillResultDto>.Ok(result,
            $"回补完成：新建 {result.CreatedCount}，跳过 {result.SkippedCount}");
    }

    /// <summary>
    /// 确认前防漏单校验
    /// 重复检查阻止确认；前置连续性、异常数据为提醒，不阻止
    /// </summary>
    public async Task<ApiResponseDto<SettlementValidationResultDto>> ValidateBeforeConfirmAsync(long id)
    {
        var ctx = ResolveTenantStore();
        if (ctx.Error != null)
            return ApiResponseDto<SettlementValidationResultDto>.Fail(ctx.Error, ctx.Code);

        var entity = await GetOwnedAsync(ctx, id);
        if (entity == null)
            return ApiResponseDto<SettlementValidationResultDto>.Fail("日结记录不存在", 404);

        var result = new SettlementValidationResultDto();

        // 重复检查：已确认则阻止
        if (entity.Status == 1)
        {
            result.CanConfirm = false;
            result.Warnings.Add("该日期已确认日结，不可重复确认");
            return ApiResponseDto<SettlementValidationResultDto>.Ok(result);
        }
        result.CanConfirm = true;

        // 前置连续性：从门店开业日期起所有前序日期均应有日结记录（P-DS-06）
        // 开业日期取门店首次订单日期，若无订单则用当前日结日期兜底
        var storeStartDate = await _dbContext.Orders
            .Where(o => o.TenantId == ctx.TenantId && o.StoreId == ctx.StoreId)
            .MinAsync(o => (DateTime?)o.OrderTime) ?? entity.SettlementDate;

        var expectedDays = (entity.SettlementDate.Date - storeStartDate.Date).Days;
        if (expectedDays > 0)
        {
            var existingDates = await _dbContext.DailySettlements
                .Where(s => s.TenantId == ctx.TenantId && s.StoreId == ctx.StoreId
                    && s.SettlementDate >= storeStartDate.Date && s.SettlementDate < entity.SettlementDate)
                .Select(s => s.SettlementDate.Date)
                .Distinct()
                .ToListAsync();

            var existingSet = existingDates.ToHashSet();
            var missingDates = new List<DateTime>();
            for (var d = storeStartDate.Date; d < entity.SettlementDate.Date; d = d.AddDays(1))
            {
                if (!existingSet.Contains(d))
                    missingDates.Add(d);
            }

            if (missingDates.Count > 0)
            {
                result.Warnings.Add($"开业以来共 {missingDates.Count} 天未日结，请确认是否有遗漏");
                // 最多返回 30 天缺失日期，便于前端日历视图展示
                result.MissingDates = missingDates.Take(30).ToList();
            }
        }

        // 异常数据：当日无订单则提醒
        if (entity.OrderCount == 0)
            result.Warnings.Add("当日无订单，请确认是否有遗漏");

        // 退款大于营收：区分"有营收但退款更大"和"无营收但有退款"两种情形（提醒级，不阻止）
        if (entity.TotalRevenue == 0 && entity.TotalRefund > 0)
            result.Warnings.Add($"当日无营收但有退款 ¥{entity.TotalRefund:F2}，请确认是否漏录订单");
        else if (entity.TotalRefund > entity.TotalRevenue)
            result.Warnings.Add($"当日退款 ¥{entity.TotalRefund:F2} 大于营收 ¥{entity.TotalRevenue:F2}，请确认是否有异常退款");

        return ApiResponseDto<SettlementValidationResultDto>.Ok(result);
    }

    /// <summary>
    /// 获取日结记录分页列表
    /// </summary>
    public async Task<ApiResponseDto<PagedResponseDto<DailySettlementDto>>> GetPagedListAsync(DailySettlementQueryDto query)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<PagedResponseDto<DailySettlementDto>>.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;
        var queryable = _dbContext.DailySettlements
            .Where(s => s.TenantId == tenantId);

        // 门店过滤：优先查询参数，其次当前用户门店
        var storeId = query.StoreId ?? _currentUser.StoreId;
        if (storeId.HasValue)
            queryable = queryable.Where(s => s.StoreId == storeId.Value);

        if (query.SettlementDateStart.HasValue)
            queryable = queryable.Where(s => s.SettlementDate >= query.SettlementDateStart.Value);
        if (query.SettlementDateEnd.HasValue)
            queryable = queryable.Where(s => s.SettlementDate <= query.SettlementDateEnd.Value);
        if (query.Status.HasValue)
            queryable = queryable.Where(s => s.Status == query.Status.Value);

        var total = await queryable.CountAsync();
        var items = await queryable
            .OrderByDescending(s => s.SettlementDate)
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var result = new PagedResponseDto<DailySettlementDto>
        {
            List = items.Adapt<List<DailySettlementDto>>(),
            Total = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
        return ApiResponseDto<PagedResponseDto<DailySettlementDto>>.Ok(result);
    }

    /// <summary>
    /// 根据ID获取日结记录详情
    /// </summary>
    public async Task<ApiResponseDto<DailySettlementDto?>> GetByIdAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<DailySettlementDto?>.Fail("登录状态异常，请重新登录", 401);

        var queryable = _dbContext.DailySettlements
            .Where(s => s.Id == id && s.TenantId == _currentUser.TenantId.Value);
        if (_currentUser.StoreId.HasValue)
            queryable = queryable.Where(s => s.StoreId == _currentUser.StoreId.Value);

        var entity = await queryable.FirstOrDefaultAsync();
        if (entity == null)
            return ApiResponseDto<DailySettlementDto?>.Fail("日结记录不存在", 404);
        return ApiResponseDto<DailySettlementDto?>.Ok(entity.Adapt<DailySettlementDto>());
    }

    /// <summary>
    /// 解析当前租户与门店
    /// </summary>
    private TenantStoreContext ResolveTenantStore()
    {
        if (!_currentUser.TenantId.HasValue)
            return new TenantStoreContext(0, 0, "登录状态异常，请重新登录", 401);
        if (!_currentUser.StoreId.HasValue)
            return new TenantStoreContext(0, 0, "请选择门店", 400);
        return new TenantStoreContext(_currentUser.TenantId.Value, _currentUser.StoreId.Value, null, 0);
    }

    /// <summary>
    /// 获取当前门店下属的日结记录
    /// </summary>
    private Task<DailySettlementEntity?> GetOwnedAsync(TenantStoreContext ctx, long id)
        => _dbContext.DailySettlements
            .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == ctx.TenantId && s.StoreId == ctx.StoreId);

    /// <summary>
    /// 查询门店编码
    /// </summary>
    private async Task<string> GetStoreCodeAsync(long tenantId, long storeId)
    {
        var code = await _dbContext.Stores
            .Where(s => s.TenantId == tenantId && s.Id == storeId)
            .Select(s => s.Code)
            .FirstOrDefaultAsync();
        return code ?? string.Empty;
    }

    /// <summary>
    /// 租户门店上下文
    /// </summary>
    private record TenantStoreContext(long TenantId, long StoreId, string? Error, int Code);
}
