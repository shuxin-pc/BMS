using FluentValidation;
using Bms.Store.Application.Dtos.Technicians;

namespace Bms.Store.Application.Validators;

/// <summary>
/// 创建技师统计请求验证器
/// </summary>
public class TechnicianStatisticCreateDtoValidator : AbstractValidator<TechnicianStatisticCreateDto>
{
    public TechnicianStatisticCreateDtoValidator()
    {
        RuleFor(x => x.TechnicianId).GreaterThan(0).WithMessage("技师ID必须大于0");
        RuleFor(x => x.ServiceCount).GreaterThanOrEqualTo(0).WithMessage("服务次数必须大于等于0");
        RuleFor(x => x.ServiceMinutes).GreaterThanOrEqualTo(0).WithMessage("服务时长必须大于等于0");
        RuleFor(x => x.TotalTechnicianFee).GreaterThanOrEqualTo(0).WithMessage("技师服务费用汇总必须大于等于0");
        RuleFor(x => x.ReturnCustomerCount).GreaterThanOrEqualTo(0).WithMessage("回头客数量必须大于等于0");
    }
}

/// <summary>
/// 更新技师统计请求验证器
/// </summary>
public class TechnicianStatisticUpdateDtoValidator : AbstractValidator<TechnicianStatisticUpdateDto>
{
    public TechnicianStatisticUpdateDtoValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("ID无效");
        Include(new TechnicianStatisticCreateDtoValidator());
    }
}
