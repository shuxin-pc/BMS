using Bms.BuildingBlocks.Abstractions.Security;
using Bms.BuildingBlocks.Core.Context;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.PosCheckouts;
using Bms.Store.Application.Dtos.TreatmentCards;
using Bms.Store.Infrastructure;

namespace Bms.Store.Application.Services;

/// <summary>
/// POS 快速开单混合结算应用服务实现
/// 复用 OrderAppService / TreatmentCardVerifyAppService / TreatmentCardSaleAppService 的 CreateAsync
/// （三者已支持外部事务复用：检测到已存在事务时不重复开事务），
/// 由本服务开启唯一事务并统一提交/回滚，保证核销+建单+开卡的原子性。
/// </summary>
public class PosCheckoutAppService : IPosCheckoutAppService
{
    private readonly StoreDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IOrderAppService _orderService;
    private readonly ITreatmentCardVerifyAppService _verifyService;
    private readonly ITreatmentCardSaleAppService _saleService;
    private readonly IAuditLogContext _auditLogContext;

    public PosCheckoutAppService(
        StoreDbContext dbContext,
        ICurrentUser currentUser,
        IOrderAppService orderService,
        ITreatmentCardVerifyAppService verifyService,
        ITreatmentCardSaleAppService saleService,
        IAuditLogContext auditLogContext)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _orderService = orderService;
        _verifyService = verifyService;
        _saleService = saleService;
        _auditLogContext = auditLogContext;
    }

    public async Task<ApiResponseDto<PosCheckoutResultDto>> CheckoutAsync(PosCheckoutCreateDto dto)
    {
        // 审计日志语义化：标记业务动作类型
        _auditLogContext.CustomOperationType = "混合结算";

        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<PosCheckoutResultDto>.Fail("登录状态异常，请重新登录", 401);

        // 结算内容至少包含一类单据（商品/服务行、核销行、开卡行至少其一）
        var hasOrder = dto.Order != null && (dto.Order.Items?.Any() ?? false);
        var hasVerify = dto.Verifies?.Any(v => v.Items?.Any() == true) ?? false;
        var hasSale = dto.Sales?.Any() ?? false;
        if (!hasOrder && !hasVerify && !hasSale)
            return ApiResponseDto<PosCheckoutResultDto>.Fail("结算内容为空", 400);

        // 组合支付（PayMethod=7）校验与三栏归一（POS 混合结算专用）：
        // 前端三栏按"整单应收"拆分（含开卡费等独立记账渠道费用），而订单 PaidAmount 仅含商品/服务行，
        // 因此校验基准 = PaidAmount + 其他渠道费用（开卡费合计），保证前后端应付金额一致；
        // 校验通过后将整单三栏拆分为"订单部分"与"开卡部分"并各自归一（末栏补差保证合计精确）：
        //   - 有订单：订单三栏按 PaidAmount/整单应付比例压缩（合计 = PaidAmount），开卡三栏 = 整单三栏 − 订单三栏（合计 = 开卡费）
        //   - 纯开卡：整单三栏即开卡部分，按各开卡销售金额占比分摊到每张卡（前端所有开卡行传入相同整单三栏，必须分摊避免多张卡重复扣减）
        // 使 Order/TreatmentCardSale 表三栏各自自洽（日结营收/退款分摊/积分基数/累计消费依赖，见 D3），
        // 且"整单三栏总和 = 填写金额"不变 —— 订单部分由 OrderAppService 扣减、开卡部分由 TreatmentCardSaleAppService 扣减，
        // 合计实现"弹窗填多少就真扣多少"
        var hasCombinedPay = (hasOrder && dto.Order!.PayMethod == 7)
                             || (hasSale && dto.Sales!.Any(s => s.PayMethod == 7));
        if (hasCombinedPay)
        {
            var order = dto.Order;
            var sales = dto.Sales ?? new List<TreatmentCardSaleCreateDto>();
            // 整单三栏基准：前端订单与各开卡行传入相同的完整三栏；纯开卡（无订单）时取首张开卡行
            var firstSale = sales.FirstOrDefault();
            var cash = order?.CashAmount ?? firstSale?.CashAmount ?? 0m;
            var sv = order?.StoredValueAmount ?? firstSale?.StoredValueAmount ?? 0m;
            var points = order?.PointsAmount ?? firstSale?.PointsAmount ?? 0m;
            var orderPaid = order?.PaidAmount ?? 0m;
            // 其他渠道费用：开卡费（TreatmentCardSale.Amount）独立记账、不进订单/日结，但需计入整单应收校验
            var otherChannelAmount = sales.Sum(s => s.Amount);
            var totalPayable = orderPaid + otherChannelAmount;

            var isValid = cash >= 0 && sv >= 0 && points >= 0
                          && (cash + sv + points) > 0
                          && Math.Abs(cash + sv + points - totalPayable) < 0.01m;
            if (!isValid)
                return ApiResponseDto<PosCheckoutResultDto>.Fail(
                    $"组合支付字段不合法：三项总和必须等于应付金额（订单实收 {orderPaid:F2} + 其他渠道费用 {otherChannelAmount:F2} = {totalPayable:F2}，精度0.01），各项≥0，至少一项>0", 400);

            var cashPayMethod = order?.CashPayMethod ?? firstSale?.CashPayMethod;
            if (cash > 0 && (cashPayMethod == null || cashPayMethod < 1 || cashPayMethod > 4))
                return ApiResponseDto<PosCheckoutResultDto>.Fail("组合支付 CashAmount>0 时 CashPayMethod 必填且仅 1(现金)/2(支付宝)/3(微信)/4(银行卡) 有效", 400);

            // 有订单：订单三栏按下单实收/整单应付比例压缩（储值/积分按比例四舍五入，现金栏补差）；
            // 开卡部分三栏 = 整单三栏 − 订单三栏（合计 = 开卡费），随后按各开卡销售金额占比分摊
            if (order != null && totalPayable > 0 && orderPaid > 0)
            {
                var ratio = orderPaid / totalPayable;
                var svScaled = Math.Round(sv * ratio, 2, MidpointRounding.AwayFromZero);
                var pointsScaled = Math.Round(points * ratio, 2, MidpointRounding.AwayFromZero);
                var cashScaled = Math.Round(orderPaid - svScaled - pointsScaled, 2, MidpointRounding.AwayFromZero);
                order.CashAmount = cashScaled;
                order.StoredValueAmount = svScaled;
                order.PointsAmount = pointsScaled;

                cash -= cashScaled;
                sv -= svScaled;
                points -= pointsScaled;
            }
            else if (order != null)
            {
                // 订单实收为 0（商品免单 + 开卡收费）：订单三栏清零（订单不收款），
                // 避免 OrderAppService 按三栏合计 == PaidAmount(0) 校验失败；整单三栏全部分配给开卡部分
                order.CashAmount = 0m;
                order.StoredValueAmount = 0m;
                order.PointsAmount = 0m;
            }
            // 纯开卡（无订单）：整单三栏即开卡部分，三栏基准保持不变

            // 开卡部分三栏按各开卡销售金额占比分摊（现金栏补差，保证每张卡三栏合计 = 该卡 Amount）
            var totalSaleAmount = sales.Sum(s => s.Amount);
            if (totalSaleAmount > 0)
            {
                foreach (var sale in sales)
                {
                    var saleRatio = sale.Amount / totalSaleAmount;
                    var saleSv = Math.Round(sv * saleRatio, 2, MidpointRounding.AwayFromZero);
                    var salePoints = Math.Round(points * saleRatio, 2, MidpointRounding.AwayFromZero);
                    var saleCash = Math.Round(sale.Amount - saleSv - salePoints, 2, MidpointRounding.AwayFromZero);
                    if (saleCash < 0) saleCash = 0m;
                    sale.CashAmount = saleCash;
                    sale.StoredValueAmount = saleSv;
                    sale.PointsAmount = salePoints;
                }
            }
        }

        // 唯一事务包裹三类单据，任一失败整体回滚
        await using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            var result = new PosCheckoutResultDto
            {
                CheckoutSessionNo = dto.CheckoutSessionNo ?? string.Empty
            };

            // 1) 核销优先（D2 结算时序：先核销、后建单、再开卡）
            // 同一张卡多次加入购物车时前端已按 CardSaleId 聚合为一项，此处逐项核销
            if (hasVerify)
            {
                foreach (var verifyItem in dto.Verifies!)
                {
                    var verifyResult = await _verifyService.CreateAsync(new TreatmentCardVerifyCreateDto
                    {
                        CardSaleId = verifyItem.CardSaleId,
                        Items = verifyItem.Items,
                        CheckoutSessionNo = dto.CheckoutSessionNo
                    });
                    if (!verifyResult.IsSuccess)
                    {
                        await transaction.RollbackAsync();
                        return ApiResponseDto<PosCheckoutResultDto>.Fail(verifyResult.Message ?? "项目卡核销失败", verifyResult.Code);
                    }
                    result.Verifies.Add(new PosCheckoutVerifyResultDto
                    {
                        CardSaleId = verifyItem.CardSaleId,
                        Times = verifyResult.Data?.VerifyTimes ?? 0,
                        Amount = verifyResult.Data?.VerifyAmount ?? 0m
                    });
                }
            }

            // 2) 商品/服务行建订单（系统收款记录仅覆盖商品/服务，开卡费不进订单）
            if (hasOrder)
            {
                var orderResult = await _orderService.CreateAsync(dto.Order!);
                if (!orderResult.IsSuccess)
                {
                    await transaction.RollbackAsync();
                    return ApiResponseDto<PosCheckoutResultDto>.Fail(orderResult.Message ?? "订单创建失败", orderResult.Code);
                }
                result.Orders.Add(new PosCheckoutOrderResultDto
                {
                    OrderId = orderResult.Data?.Id ?? 0,
                    OrderNo = orderResult.Data?.OrderNo ?? string.Empty,
                    Amount = orderResult.Data?.PaidAmount ?? 0m
                });
            }

            // 3) 开卡独立记账（金额为线下独立收款，不进系统支付/日结）
            if (hasSale)
            {
                foreach (var saleDto in dto.Sales!)
                {
                    var saleResult = await _saleService.CreateAsync(saleDto);
                    if (!saleResult.IsSuccess)
                    {
                        await transaction.RollbackAsync();
                        return ApiResponseDto<PosCheckoutResultDto>.Fail(saleResult.Message ?? "项目卡开卡失败", saleResult.Code);
                    }
                    result.Sales.Add(new PosCheckoutSaleResultDto
                    {
                        SaleId = saleResult.Data?.Id ?? 0,
                        Amount = saleResult.Data?.Amount ?? 0m
                    });
                }
            }

            await transaction.CommitAsync();
            return ApiResponseDto<PosCheckoutResultDto>.Ok(result, "结算成功");
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
