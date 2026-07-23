using FluentValidation;
using Bms.Store.Application.Dtos.Orders;

namespace Bms.Store.Application.Validators;

/// <summary>
/// 创建订单请求验证器
/// </summary>
public class OrderCreateDtoValidator : AbstractValidator<OrderCreateDto>
{
    public OrderCreateDtoValidator()
    {
        RuleFor(x => x.OrderNo).NotEmpty().WithMessage("订单号不能为空").MaximumLength(50).WithMessage("订单号最多50个字符");
        RuleFor(x => x.OrderType).Must(t => t == 1 || t == 2 || t == 3).WithMessage("订单类型只能为1(零售)、2(服务)或3(疗程卡核销)");
        RuleFor(x => x.Status).Must(s => s == 1 || s == 2 || s == 3 || s == 4).WithMessage("订单状态只能为1(进行中)、2(已完成)、3(已退款)或4(已取消)");
        RuleFor(x => x.PayMethod).NotNull().WithMessage("支付方式不能为空")
            .Must(p => p == 1 || p == 2 || p == 3 || p == 4 || p == 5 || p == 6 || p == 7)
            .WithMessage("支付方式只能为1(现金)、2(支付宝)、3(微信)、4(银行卡)、5(储值卡)、6(积分抵扣)或7(组合支付)");
        RuleFor(x => x.ProductAmount).GreaterThanOrEqualTo(0).WithMessage("商品总金额不能小于0");
        RuleFor(x => x.PaidAmount).GreaterThanOrEqualTo(0).WithMessage("实收金额不能小于0");

        // 组合支付字段校验（仅 PayMethod=7 时生效）
        RuleFor(x => x).Must(BeValidCombinedPayment).When(x => x.PayMethod == 7)
            .WithMessage("组合支付字段不合法：三项总和必须等于 PaidAmount（精度0.01），各项≥0，CashPayMethod 仅 1-4 有效，至少一项>0");
        RuleFor(x => x.CashPayMethod).Must(p => p == 1 || p == 2 || p == 3 || p == 4)
            .When(x => x.PayMethod == 7 && x.CashAmount > 0)
            .WithMessage("组合支付 CashAmount>0 时 CashPayMethod 必填且仅 1(现金)/2(支付宝)/3(微信)/4(银行卡) 有效");
    }

    /// <summary>
    /// 校验组合支付字段：三栏总和=PaidAmount、各项≥0、至少一项>0
    /// </summary>
    private static bool BeValidCombinedPayment(OrderCreateDto dto)
    {
        var cash = dto.CashAmount ?? 0m;
        var sv = dto.StoredValueAmount ?? 0m;
        var points = dto.PointsAmount ?? 0m;

        if (cash < 0 || sv < 0 || points < 0) return false;
        if (cash == 0 && sv == 0 && points == 0) return false;

        // 精度 0.01 容差
        return Math.Abs(cash + sv + points - dto.PaidAmount) < 0.01m;
    }
}

/// <summary>
/// 取消订单请求验证器
/// </summary>
public class OrderCancelDtoValidator : AbstractValidator<OrderCancelDto>
{
    public OrderCancelDtoValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().WithMessage("取消原因不能为空").MaximumLength(500).WithMessage("取消原因最多500个字符");
    }
}

/// <summary>
/// 订单退款请求验证器
/// </summary>
public class RefundRequestDtoValidator : AbstractValidator<RefundRequestDto>
{
    public RefundRequestDtoValidator()
    {
        RuleFor(x => x.OrderId).GreaterThan(0).WithMessage("订单ID无效");
        // 允许 RefundAmount=0（OrderType=3 疗程卡核销订单退款时为0），具体业务校验由 RefundAsync 按 OrderType 区分
        RuleFor(x => x.RefundAmount).GreaterThanOrEqualTo(0).WithMessage("退款金额不能小于0");
        RuleFor(x => x.Reason).NotEmpty().WithMessage("退款原因不能为空").MaximumLength(500).WithMessage("退款原因最多500个字符");
    }
}
