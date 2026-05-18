using FluentValidation;
using Bms.System.Application.Dtos.Users;

namespace Bms.System.Application.Validators;

/// <summary>
/// 用户创建 DTO 验证器
/// </summary>
public class UserCreateDtoValidator : AbstractValidator<UserCreateDto>
{
    public UserCreateDtoValidator()
    {
        RuleFor(x => x.UserName)
            .NotEmpty().WithMessage("用户名不能为空")
            .MinimumLength(3).WithMessage("用户名至少3个字符")
            .MaximumLength(50).WithMessage("用户名最多50个字符")
            .Matches(@"^[a-zA-Z0-9_]+$").WithMessage("用户名只能包含字母、数字、下划线");

        RuleFor(x => x.RealName)
            .NotEmpty().WithMessage("真实姓名不能为空")
            .MaximumLength(50).WithMessage("真实姓名最多50个字符");

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("邮箱格式不正确")
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.Phone)
            .Matches(@"^1[3-9]\d{9}$").WithMessage("手机号格式不正确")
            .When(x => !string.IsNullOrEmpty(x.Phone));

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("密码不能为空")
            .MinimumLength(6).WithMessage("密码至少6个字符")
            .MaximumLength(100).WithMessage("密码最多100个字符");

        RuleFor(x => x.Status)
            .InclusiveBetween(0, 1).WithMessage("状态只能是0或1");

        RuleFor(x => x.OrganizationId)
            .GreaterThan(0).WithMessage("组织ID必须大于0")
            .When(x => x.OrganizationId.HasValue);

        RuleFor(x => x.RoleIds)
            .Must(x => x == null || x.Count > 0).WithMessage("必须至少选择一个角色")
            .When(x => x.RoleIds != null);
    }
}

/// <summary>
/// 用户更新 DTO 验证器
/// </summary>
public class UserUpdateDtoValidator : AbstractValidator<UserUpdateDto>
{
    public UserUpdateDtoValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("用户ID无效");

        RuleFor(x => x.UserName)
            .NotEmpty().WithMessage("用户名不能为空")
            .MinimumLength(3).WithMessage("用户名至少3个字符")
            .MaximumLength(50).WithMessage("用户名最多50个字符")
            .Matches(@"^[a-zA-Z0-9_]+$").WithMessage("用户名只能包含字母、数字、下划线");

        RuleFor(x => x.RealName)
            .NotEmpty().WithMessage("真实姓名不能为空")
            .MaximumLength(50).WithMessage("真实姓名最多50个字符");

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("邮箱格式不正确")
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.Phone)
            .Matches(@"^1[3-9]\d{9}$").WithMessage("手机号格式不正确")
            .When(x => !string.IsNullOrEmpty(x.Phone));

        RuleFor(x => x.Status)
            .InclusiveBetween(0, 1).WithMessage("状态只能是0或1");

        RuleFor(x => x.OrganizationId)
            .GreaterThan(0).WithMessage("组织ID必须大于0")
            .When(x => x.OrganizationId.HasValue);
    }
}

/// <summary>
/// 用户密码 DTO 验证器
/// </summary>
public class UserPasswordDtoValidator : AbstractValidator<UserPasswordDto>
{
    public UserPasswordDtoValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("用户ID无效");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("新密码不能为空")
            .MinimumLength(6).WithMessage("新密码至少6个字符")
            .MaximumLength(100).WithMessage("新密码最多100个字符");
    }
}

/// <summary>
/// 用户角色分配 DTO 验证器
/// </summary>
public class UserAssignRolesDtoValidator : AbstractValidator<UserAssignRolesDto>
{
    public UserAssignRolesDtoValidator()
    {
        RuleFor(x => x.UserId)
            .GreaterThan(0).WithMessage("用户ID无效");

        RuleFor(x => x.RoleIds)
            .Must(x => x != null && x.Count > 0).WithMessage("必须至少选择一个角色");
    }
}