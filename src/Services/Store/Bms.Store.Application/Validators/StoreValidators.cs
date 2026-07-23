using FluentValidation;
using Bms.Store.Application.Dtos.Stores;

namespace Bms.Store.Application.Validators;

/// <summary>
/// 创建门店请求验证器（规则对齐前端表单校验）
/// </summary>
public class StoreCreateDtoValidator : AbstractValidator<StoreCreateDto>
{
    public StoreCreateDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("门店名称不能为空")
            .MaximumLength(100).WithMessage("门店名称最多100个字符");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("门店编号不能为空")
            .MinimumLength(2).WithMessage("门店编号长度为2-50个字符")
            .MaximumLength(50).WithMessage("门店编号长度为2-50个字符")
            .Matches(@"^[a-zA-Z0-9_]+$").WithMessage("门店编号只能包含字母、数字、下划线");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("联系电话不能为空")
            .Matches(@"^1[3-9]\d{9}$").WithMessage("手机号格式不正确");

        RuleFor(x => x.ManagerName)
            .NotEmpty().WithMessage("店长姓名不能为空")
            .MaximumLength(50).WithMessage("店长姓名最多50个字符");

        RuleFor(x => x.Status)
            .Must(s => s == 1 || s == 2).WithMessage("门店状态只能为1(营业)或2(歇业)");
    }
}

/// <summary>
/// 更新门店请求验证器
/// </summary>
public class StoreUpdateDtoValidator : AbstractValidator<StoreUpdateDto>
{
    public StoreUpdateDtoValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("门店ID无效");

        Include(new StoreCreateDtoValidator());
    }
}
