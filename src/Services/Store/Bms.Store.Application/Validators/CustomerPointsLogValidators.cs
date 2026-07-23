using FluentValidation;
using Bms.Store.Application.Dtos.Customers;
using Bms.Store.Domain.Constants;

namespace Bms.Store.Application.Validators;

/// <summary>
/// 创建客户积分流水 DTO 校验器
/// 按 Type 区分校验：手动调整（Type=8）必须填写 Remark（>= 5 字符）且 Points != 0
/// 过期清零（Type=7）允许 OperatorId 为 null（系统操作）
/// </summary>
public class CustomerPointsLogCreateDtoValidator : AbstractValidator<CustomerPointsLogCreateDto>
{
    public CustomerPointsLogCreateDtoValidator()
    {
        RuleFor(x => x.CustomerId).GreaterThan(0).WithMessage("请选择客户");

        // 类型合法范围：1-8（参见 CustomerPointsLogType）
        RuleFor(x => x.Type)
            .Must(CustomerPointsLogType.IsValid)
            .WithMessage("积分变动类型取值无效（合法范围 1-8）");

        // 变动数量不能为 0
        RuleFor(x => x.Points)
            .NotEqual(0).WithMessage("积分变动数量不能为0");

        // 余额非负
        RuleFor(x => x.BeforePoints).GreaterThanOrEqualTo(0).WithMessage("变动前积分不能为负");
        RuleFor(x => x.AfterPoints).GreaterThanOrEqualTo(0).WithMessage("变动后积分不能为负");

        // 一致性：AfterPoints = BeforePoints + Points
        RuleFor(x => x)
            .Must(x => x.AfterPoints == x.BeforePoints + x.Points)
            .WithMessage("积分变动不一致：AfterPoints 必须等于 BeforePoints + Points");

        // 操作人必填（过期清零 Type=7 允许为 null，由后台任务填 0）
        RuleFor(x => x.OperatorId)
            .NotNull().WithMessage("操作人不能为空")
            .When(x => x.Type != CustomerPointsLogType.Expire);

        // 手动调整（Type=8）：必须填写原因（>= 5 字符）
        RuleFor(x => x.Remark)
            .NotEmpty().WithMessage("手动调整积分必须填写原因")
            .MinimumLength(5).WithMessage("调整原因至少 5 个字符")
            .When(x => x.Type == CustomerPointsLogType.ManualAdjust);
    }
}

public class CustomerPointsLogUpdateDtoValidator : AbstractValidator<CustomerPointsLogUpdateDto>
{
    public CustomerPointsLogUpdateDtoValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("ID无效");
        Include(new CustomerPointsLogCreateDtoValidator());
    }
}
