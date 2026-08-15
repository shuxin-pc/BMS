using FluentValidation;
using Bms.System.Application.Dtos.Roles;

namespace Bms.System.Application.Validators;

/// <summary>
/// 角色创建 DTO 验证器
/// </summary>
public class RoleCreateDtoValidator : AbstractValidator<RoleCreateDto>
{
    public RoleCreateDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("角色名称不能为空")
            .MaximumLength(50).WithMessage("角色名称最多50个字符");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("角色编码不能为空")
            .MaximumLength(50).WithMessage("角色编码最多50个字符")
            .Matches(@"^[a-zA-Z0-9_]+$").WithMessage("角色编码只能包含字母、数字、下划线");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("描述最多500个字符");

        RuleFor(x => x.Status)
            .InclusiveBetween(0, 1).WithMessage("状态只能是0或1");

        RuleFor(x => x.Level)
            .InclusiveBetween(2, 99).WithMessage("角色等级必须在 2-99 之间（数字越小权限越大，0/1 为系统保留角色）");

        RuleFor(x => x.DataScopeType)
            .InclusiveBetween(1, 4).WithMessage("数据范围类型只能是1-4");
    }
}

/// <summary>
/// 角色更新 DTO 验证器
/// </summary>
public class RoleUpdateDtoValidator : AbstractValidator<RoleUpdateDto>
{
    public RoleUpdateDtoValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("角色ID无效");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("角色名称不能为空")
            .MaximumLength(50).WithMessage("角色名称最多50个字符");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("角色编码不能为空")
            .MaximumLength(50).WithMessage("角色编码最多50个字符")
            .Matches(@"^[a-zA-Z0-9_]+$").WithMessage("角色编码只能包含字母、数字、下划线");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("描述最多500个字符");

        RuleFor(x => x.Status)
            .InclusiveBetween(0, 1).WithMessage("状态只能是0或1");

        RuleFor(x => x.Level)
            .InclusiveBetween(2, 99).WithMessage("角色等级必须在 2-99 之间（数字越小权限越大，0/1 为系统保留角色）");

        RuleFor(x => x.DataScopeType)
            .InclusiveBetween(1, 4).WithMessage("数据范围类型只能是1-4");
    }
}