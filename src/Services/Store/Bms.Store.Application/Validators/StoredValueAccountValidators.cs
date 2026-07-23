using FluentValidation;
using Bms.Store.Application.Dtos.StoredValues;

namespace Bms.Store.Application.Validators;

/// <summary>
/// 创建储值账户请求验证器
/// </summary>
public class StoredValueAccountCreateDtoValidator : AbstractValidator<StoredValueAccountCreateDto>
{
    public StoredValueAccountCreateDtoValidator()
    {
        RuleFor(x => x.CustomerId)
            .GreaterThan(0).WithMessage("客户ID必须大于0");
    }
}

/// <summary>
/// 更新储值账户请求验证器
/// </summary>
public class StoredValueAccountUpdateDtoValidator : AbstractValidator<StoredValueAccountUpdateDto>
{
    public StoredValueAccountUpdateDtoValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("ID无效");
        Include(new StoredValueAccountCreateDtoValidator());
    }
}
