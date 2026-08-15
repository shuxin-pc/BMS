using FluentValidation;
using Bms.Store.Application.Dtos.Customers;
using Bms.Store.Domain.Constants;

namespace Bms.Store.Application.Validators;

/// <summary>
/// 创建客户积分流水 DTO 校验器
/// 按 Type 区分校验：手动调整（Type=8）必须填写 Remark 且 Points != 0
/// </summary>
public class CustomerPointsLogCreateDtoValidator : AbstractValidator<CustomerPointsLogCreateDto>
{
    public CustomerPointsLogCreateDtoValidator()
    {
        RuleFor(x => x.CustomerId).GreaterThan(0).WithMessage("请选择客户");

        // 类型合法范围：1,2,3,5,6,7,8（4 为已移除的 ActivityGift 空洞，参见 CustomerPointsLogType）
        RuleFor(x => x.Type)
            .Must(CustomerPointsLogType.IsValid)
            .WithMessage("积分变动类型取值无效（合法值：1,2,3,5,6,7,8）");

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

        // 注意：OperatorId 不做校验，由服务层强制填充为当前登录用户，避免前端伪造操作人

        // 手动调整（Type=8）：必须填写原因
        RuleFor(x => x.Remark)
            .NotEmpty().WithMessage("手动调整积分必须填写原因")
            .When(x => x.Type == CustomerPointsLogType.ManualAdjust);
    }
}

public class CustomerPointsLogUpdateDtoValidator : AbstractValidator<CustomerPointsLogUpdateDto>
{
    public CustomerPointsLogUpdateDtoValidator()
    {
        Include(new CustomerPointsLogCreateDtoValidator());
    }
}
