using FluentValidation;
using Bms.BuildingBlocks.MultiTenant.Models;
using Bms.System.Application.Dtos.Tenants;

namespace Bms.System.Application.Validators;

/// <summary>
/// 租户创建 DTO 验证器
/// </summary>
public class TenantCreateDtoValidator : AbstractValidator<TenantCreateDto>
{
    public TenantCreateDtoValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("租户编码不能为空")
            .MinimumLength(2).WithMessage("租户编码至少2个字符")
            .MaximumLength(50).WithMessage("租户编码最多50个字符")
            .Matches(@"^[a-zA-Z0-9_]+$").WithMessage("租户编码只能包含字母、数字、下划线");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("租户名称不能为空")
            .MaximumLength(100).WithMessage("租户名称最多100个字符");

        RuleFor(x => x.ContactName)
            .MaximumLength(50).WithMessage("联系人最多50个字符");

        RuleFor(x => x.ContactPhone)
            .Matches(@"^1[3-9]\d{9}$").WithMessage("手机号格式不正确")
            .When(x => !string.IsNullOrEmpty(x.ContactPhone));

        RuleFor(x => x.ContactEmail)
            .EmailAddress().WithMessage("邮箱格式不正确")
            .When(x => !string.IsNullOrEmpty(x.ContactEmail));

        RuleFor(x => x.Status)
            .InclusiveBetween(0, 1).WithMessage("状态只能是0或1");

        RuleFor(x => (int)x.IsolationLevel)
            .InclusiveBetween(1, 3).WithMessage("隔离级别只能是1-3");

        RuleFor(x => x.ConnectionString)
            .NotEmpty().WithMessage("数据库连接字符串不能为空")
            .When(x => x.IsolationLevel == TenantIsolationLevel.Database);

        RuleFor(x => x.SchemaName)
            .MaximumLength(50).WithMessage("Schema名称最多50个字符")
            .Matches(@"^[a-z][a-z0-9_]*$").WithMessage("Schema名称必须以小写字母开头，只能包含小写字母、数字、下划线")
            .When(x => !string.IsNullOrEmpty(x.SchemaName));

        RuleFor(x => x.ExpireTime)
            .GreaterThan(DateTime.UtcNow).WithMessage("到期时间必须大于当前时间")
            .When(x => x.ExpireTime.HasValue);

        RuleFor(x => x.AllowedSubsystems)
            .MaximumLength(500).WithMessage("允许子系统列表最多500个字符");
    }
}

/// <summary>
/// 租户更新 DTO 验证器
/// </summary>
public class TenantUpdateDtoValidator : AbstractValidator<TenantUpdateDto>
{
    public TenantUpdateDtoValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("租户ID无效");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("租户名称不能为空")
            .MaximumLength(100).WithMessage("租户名称最多100个字符");

        RuleFor(x => x.ContactName)
            .MaximumLength(50).WithMessage("联系人最多50个字符");

        RuleFor(x => x.ContactPhone)
            .Matches(@"^1[3-9]\d{9}$").WithMessage("手机号格式不正确")
            .When(x => !string.IsNullOrEmpty(x.ContactPhone));

        RuleFor(x => x.ContactEmail)
            .EmailAddress().WithMessage("邮箱格式不正确")
            .When(x => !string.IsNullOrEmpty(x.ContactEmail));

        RuleFor(x => x.Status)
            .InclusiveBetween(0, 1).WithMessage("状态只能是0或1");

        RuleFor(x => (int)x.IsolationLevel)
            .InclusiveBetween(1, 3).WithMessage("隔离级别只能是1-3");

        RuleFor(x => x.SchemaName)
            .MaximumLength(50).WithMessage("Schema名称最多50个字符")
            .Matches(@"^[a-z][a-z0-9_]*$").WithMessage("Schema名称必须以小写字母开头，只能包含小写字母、数字、下划线")
            .When(x => !string.IsNullOrEmpty(x.SchemaName));

        RuleFor(x => x.ExpireTime)
            .GreaterThan(DateTime.UtcNow).WithMessage("到期时间必须大于当前时间")
            .When(x => x.ExpireTime.HasValue);

        RuleFor(x => x.AllowedSubsystems)
            .MaximumLength(500).WithMessage("允许子系统列表最多500个字符");
    }
}