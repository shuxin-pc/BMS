using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.Extensions.DependencyInjection;

namespace Bms.System.Application.Validators;

/// <summary>
/// FluentValidation 配置类
/// </summary>
public static class FluentValidationExtensions
{
    /// <summary>
    /// 注册 FluentValidation 服务
    /// </summary>
    public static IServiceCollection AddFluentValidationServices(this IServiceCollection services)
    {
        // 添加 FluentValidation 服务
        services.AddFluentValidationAutoValidation(options =>
        {
            // 禁用默认的 IModelValidatorProvider，因为我们要手动注册
            options.DisableDataAnnotationsValidation = false;
        });

        // 注册所有继承自 AbstractValidator 的验证器
        // 使用一个非静态类作为程序集引用参数
        services.AddValidatorsFromAssemblyContaining<UserCreateDtoValidator>();

        return services;
    }
}