using FluentValidation;
using Bms.Store.Application.Dtos.Statistics;

namespace Bms.Store.Application.Validators;

/// <summary>
/// 创建日统计请求验证器
/// </summary>
public class DailyStatCreateDtoValidator : AbstractValidator<DailyStatCreateDto>
{
    public DailyStatCreateDtoValidator()
    {
        RuleFor(x => x.OrderCount).GreaterThanOrEqualTo(0).WithMessage("订单数量必须大于等于0");
        RuleFor(x => x.Revenue).GreaterThanOrEqualTo(0).WithMessage("营收必须大于等于0");
        RuleFor(x => x.Cost).GreaterThanOrEqualTo(0).WithMessage("成本必须大于等于0");
        RuleFor(x => x.RefundAmount).GreaterThanOrEqualTo(0).WithMessage("退款金额必须大于等于0");
        RuleFor(x => x.ConsumeCustomerCount).GreaterThanOrEqualTo(0).WithMessage("消费客户数必须大于等于0");
        RuleFor(x => x.NewCustomerCount).GreaterThanOrEqualTo(0).WithMessage("新增客户数必须大于等于0");
        RuleFor(x => x.AppointmentCount).GreaterThanOrEqualTo(0).WithMessage("预约数量必须大于等于0");
        RuleFor(x => x.InventoryAlertCount).GreaterThanOrEqualTo(0).WithMessage("库存预警数必须大于等于0");
    }
}

/// <summary>
/// 更新日统计请求验证器
/// </summary>
public class DailyStatUpdateDtoValidator : AbstractValidator<DailyStatUpdateDto>
{
    public DailyStatUpdateDtoValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("ID无效");
        Include(new DailyStatCreateDtoValidator());
    }
}
