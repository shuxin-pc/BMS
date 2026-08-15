using FluentValidation;
using Bms.Store.Application.Dtos.Technicians;

namespace Bms.Store.Application.Validators;

/// <summary>
/// 创建商家技师请求验证器
/// </summary>
public class TechnicianCreateDtoValidator : AbstractValidator<TechnicianCreateDto>
{
    public TechnicianCreateDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("技师姓名不能为空")
            .MaximumLength(50).WithMessage("技师姓名最多50个字符");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("手机号不能为空")
            .Matches(@"^1[3-9]\d{9}$").WithMessage("手机号格式不正确");

        RuleFor(x => x.Gender)
            .InclusiveBetween(0, 2).WithMessage("性别只能为0(未知)、1(男)或2(女)");

        RuleFor(x => x.Status)
            .InclusiveBetween(1, 2).WithMessage("状态只能为1(在岗)或2(休息)");

        // 技师来源(Source)不由 DTO 决定，由后端根据当前租户强制赋值，无需校验
    }
}

/// <summary>
/// 更新商家技师请求验证器
/// </summary>
public class TechnicianUpdateDtoValidator : AbstractValidator<TechnicianUpdateDto>
{
    public TechnicianUpdateDtoValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("ID无效");

        Include(new TechnicianCreateDtoValidator());
    }
}
