using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Bms.BuildingBlocks.Web.Extensions;

/// <summary>
/// 认证授权扩展方法
/// </summary>
public static class AuthenticationExtensions
{
    /// <summary>
    /// 添加BMS系统JWT认证
    /// 使用与Identity.Api相同的签名密钥来验证OpenIddict生成的token
    /// </summary>
    public static IServiceCollection AddMesAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        // 与 Identity.Api 使用相同的签名密钥
        var signingKey = Convert.FromBase64String("DRjd/GnduI3Efzen9V9BvbNUfc/VKgXltV7Kbk9sMkY=");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = false,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(signingKey)
                };
                // 禁用元数据检索，避免需要连接到Identity.Api
                options.Configuration = new Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectConfiguration();
            });

        return services;
    }

    /// <summary>
    /// 添加BMS系统授权策略
    /// </summary>
    public static IServiceCollection AddMesAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy("Authenticated", policy =>
            {
                policy.RequireAuthenticatedUser();
            });

            options.AddPolicy("Admin", policy =>
            {
                policy.RequireRole("Admin");
            });
        });

        return services;
    }
}
