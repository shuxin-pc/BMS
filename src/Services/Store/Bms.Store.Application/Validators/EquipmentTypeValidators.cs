using FluentValidation;
using Bms.Store.Application.Dtos.EquipmentTypes;

namespace Bms.Store.Application.Validators;

/// <summary>
/// 新建设备类型请求验证器
/// </summary>
public class EquipmentTypeCreateDtoValidator : AbstractValidator<EquipmentTypeCreateDto>
{
    public EquipmentTypeCreateDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("类型名称不能为空")
            .MaximumLength(100).WithMessage("类型名称最多100个字符");
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("类型编码不能为空")
            .MaximumLength(50).WithMessage("类型编码最多50个字符")
            .Matches("^[A-Za-z0-9_-]+$").WithMessage("类型编码只能包含字母、数字、下划线和中划线");
        RuleFor(x => x.ParentId)
            .GreaterThan(0).WithMessage("父级类型ID无效");
        RuleFor(x => x.Spec)
            .MaximumLength(200).WithMessage("规格/型号最多200个字符");
        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("备注说明最多500个字符");
    }
}

/// <summary>
/// 更新设备类型请求验证器
/// </summary>
public class EquipmentTypeUpdateDtoValidator : AbstractValidator<EquipmentTypeUpdateDto>
{
    public EquipmentTypeUpdateDtoValidator()
    {
        Include(new EquipmentTypeCreateDtoValidator());
    }
}
