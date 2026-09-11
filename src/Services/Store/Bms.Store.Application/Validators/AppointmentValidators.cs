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
        RuleFor(x => x.CustomerId).GreaterThan(0).WithMessage("请选择客户");
        RuleFor(x => x.ProductId).GreaterThan(0).WithMessage("请选择服务项目");
        RuleFor(x => x.Status).Must(s => s == 1 || s == 2 || s == 3 || s == 4 || s == 5).WithMessage("预约状态只能为1(已预约)、2(已到店)、3(已完成)、4(已取消)或5(爽约)");
    }
}

/// <summary>
/// 更新预约请求验证器
/// </summary>
public class AppointmentUpdateDtoValidator : AbstractValidator<AppointmentUpdateDto>
{
    public AppointmentUpdateDtoValidator()
    {
        Include(new AppointmentCreateDtoValidator());
    }
}
