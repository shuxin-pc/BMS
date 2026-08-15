using Microsoft.EntityFrameworkCore;
using Bms.BuildingBlocks.Core.Extensions;
using Bms.BuildingBlocks.Web.Converters;
using Bms.Identity.Api.Data;
using Bms.Identity.Api.Services;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 配置功能开关（Feature Flags）
// ==========================================
// UseHttpsRedirection: 是否强制HTTPS重定向
//   - 开发环境: false（避免HTTP请求被重定向，方便调试）
//   - 生产环境: true（强制HTTPS，保证安全）
var useHttpsRedirection = builder.Configuration.GetValue<bool>("FeatureFlags:UseHttpsRedirection");

// ==========================================
// 配置CORS（跨域资源共享）
// ==========================================
// 开发环境: 允许前端开发服务器访问（http://localhost:5173, http://localhost:3000）
// 生产环境: 应该通过网关访问，或者配置具体的生产域名
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
        else
        {
            // 生产环境建议配置具体的允许来源
            policy.AllowAnyOrigin()
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
    });
});

// ==========================================
// 配置数据库和OpenIddict
// ==========================================
builder.Services.AddDbContext<IdentityDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("IdentityDb"));
    options.UseOpenIddict();
});

// 配置 OpenIddict
builder.Services.AddOpenIddict()
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore()
               .UseDbContext<IdentityDbContext>();
    })
    .AddServer(options =>
    {
        options.SetTokenEndpointUris("/connect/token");
        options.AllowPasswordFlow();
        options.AllowRefreshTokenFlow();
        options.AcceptAnonymousClients();
        options.DisableAccessTokenEncryption();

        // Token 有效期（L2 修复：缩短风险窗口）
        // access_token 15 分钟：用户被禁用或权限变更后，最多 15 分钟内旧 Token 仍有效
        // 配合 H8 刷新令牌机制（刷新时重新加载用户状态），实现权限变更的近实时生效
        // refresh_token 7 天：减少 refresh_token 被盗用的风险窗口
        options.SetAccessTokenLifetime(TimeSpan.FromMinutes(15));
        options.SetRefreshTokenLifetime(TimeSpan.FromDays(7));

        // 注册 System.Api 作为有效受众
        options.AddEncryptionKey(new SymmetricSecurityKey(
            Convert.FromBase64String("DRjd/GnduI3Efzen9V9BvbNUfc/VKgXltV7Kbk9sMkY=")));
        options.AddSigningKey(new SymmetricSecurityKey(
            Convert.FromBase64String("DRjd/GnduI3Efzen9V9BvbNUfc/VKgXltV7Kbk9sMkY=")));

        // 开发环境使用临时证书并允许非 HTTPS 请求
        // 生产环境应该使用真实的签名证书并强制 HTTPS
        if (builder.Environment.IsDevelopment())
        {
            options.AddDevelopmentEncryptionCertificate()
                   .AddDevelopmentSigningCertificate();
        }

        // 在开发环境中允许 HTTP 请求（不强制 HTTPS）
        options.UseAspNetCore()
               .EnableTokenEndpointPassthrough()
               .DisableTransportSecurityRequirement();
    })
    .AddValidation(options =>
    {
        options.UseLocalServer();
        options.UseAspNetCore();
    });

// ==========================================
// 配置System.Api客户端
// ==========================================
// 开发环境: 直接访问System.Api (http://localhost:5000)
// 生产环境: 应该通过服务发现或网关访问
var systemApiUrl = builder.Configuration.GetValue<string>("SystemApi:BaseUrl")
    ?? "http://localhost:5000";

// 内部服务调用凭证（与 System.Api 的 InternalServices:ServiceKey 保持一致）
var internalServiceKey = builder.Configuration["InternalServices:ServiceKey"]
    ?? string.Empty;

builder.Services.AddHttpClient<ISystemApiClient, SystemApiClient>(client =>
{
    client.BaseAddress = new Uri(systemApiUrl);
    // 添加内部服务调用认证头，通过 System.Api 的 InternalServiceAuthMiddleware 校验
    client.DefaultRequestHeaders.Add("X-Internal-Service", "IdentityService");
    client.DefaultRequestHeaders.Add("X-Internal-Service-Key", internalServiceKey);
});

// ==========================================
// 配置其他服务
// ==========================================

// 配置 JSON 序列化：将 long 类型序列化为字符串，避免 JavaScript 大整数精度丢失
// 与 System/Store 服务保持一致的 JsonConverter 方案
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new LongToStringConverter());
        options.JsonSerializerOptions.Converters.Add(new NullableLongToStringConverter());
        options.JsonSerializerOptions.Converters.Add(new IntToStringConverter());
        options.JsonSerializerOptions.Converters.Add(new NullableIntToStringConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "BMS Identity API", Version = "v1" });
});

var app = builder.Build();

// ==========================================
// 请求日志中间件（调试用，记录所有进入 Identity.Api 的请求）
// ==========================================
app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetService<ILogger<Program>>();
    logger?.LogInformation("调试：Identity.Api 收到请求 {Method} {Path}{QueryString}",
        context.Request.Method, context.Request.Path.Value, context.Request.QueryString.Value);
    await next();
    logger?.LogInformation("调试：Identity.Api 响应 {Method} {Path} -> {StatusCode}",
        context.Request.Method, context.Request.Path.Value, context.Response.StatusCode);
});

// ==========================================
// 配置中间件管道
// ==========================================
// 开发环境启用Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// HTTPS重定向（根据配置决定是否启用）
// 生产环境建议启用，开发环境可以禁用以方便调试
if (useHttpsRedirection)
{
    app.UseHttpsRedirection();
}

// CORS中间件（必须在UseAuthentication之前）
app.UseCors("AllowFrontend");

// 认证和授权中间件
app.UseAuthentication();
app.UseAuthorization();

// 路由映射
app.MapControllers();

// ==========================================
// 数据库初始化
// ==========================================
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

        logger.LogInformation("正在应用 Identity 数据库迁移...");
        context.Database.Migrate();
        logger.LogInformation("Identity 数据库迁移应用成功。");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "出现错误：初始化 Identity 数据库时发生异常，服务将继续运行。");
    }
}

app.Run();
