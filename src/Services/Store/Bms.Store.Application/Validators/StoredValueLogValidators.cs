using FluentValidation;
using Bms.Store.Application.Dtos.StoredValues;

namespace Bms.Store.Application.Validators;

/// <summary>
/// 创建储值流水请求验证器
/// </summary>
public class StoredValueLogCreateDtoValidator : AbstractValidator<StoredValueLogCreateDto>
{
    public StoredValueLogCreateDtoValidator()
    {
        RuleFor(x => x.CustomerId)
            .GreaterThan(0).WithMessage("客户ID必须大于0");

        RuleFor(x => x.Type)
            .InclusiveBetween(1, 4).WithMessage("流水类型必须是1(充值)、2(消费)、3(退款)、4(调整)");

        // 手动补录场景必填操作人；充值/消费场景由 StoredValueAccountAppService 强制填充
        RuleFor(x => x.OperatorId)
            .NotNull().WithMessage("操作人不能为空");
    }
}

/// <summary>
/// 更新储值流水请求验证器
/// </summary>
public class StoredValueLogUpdateDtoValidator : AbstractValidator<StoredValueLogUpdateDto>
{
    public StoredValueLogUpdateDtoValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("ID无效");
        Include(new StoredValueLogCreateDtoValidator());
    }
}
