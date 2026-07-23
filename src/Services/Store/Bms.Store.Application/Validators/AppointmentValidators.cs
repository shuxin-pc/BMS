using FluentValidation;
using Bms.Store.Application.Dtos.Appointments;

namespace Bms.Store.Application.Validators;

/// <summary>
/// 创建预约请求验证器
/// </summary>
public class AppointmentCreateDtoValidator : AbstractValidator<AppointmentCreateDto>
{
    public AppointmentCreateDtoValidator()
    {
        RuleFor(x => x.AppointmentNo).NotEmpty().WithMessage("预约号不能为空").MaximumLength(50).WithMessage("预约号最多50个字符");
        RuleFor(x => x.CustomerId).GreaterThan(0).WithMessage("请选择客户");
        RuleFor(x => x.ProductId).GreaterThan(0).WithMessage("请选择服务项目");
        RuleFor(x => x.Status).Must(s => s == 1 || s == 2 || s == 3 || s == 4 || s == 5 || s == 6).WithMessage("预约状态只能为1(待确认)、2(已预约)、3(已到店)、4(已完成)、5(已取消)或6(爽约)");
    }
}

/// <summary>
/// 更新预约请求验证器
/// </summary>
public class AppointmentUpdateDtoValidator : AbstractValidator<AppointmentUpdateDto>
{
    public AppointmentUpdateDtoValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("ID无效");
        Include(new AppointmentCreateDtoValidator());
    }
}
