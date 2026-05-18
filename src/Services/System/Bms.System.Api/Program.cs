using Microsoft.EntityFrameworkCore;
using Bms.BuildingBlocks.Core.Extensions;
using Bms.BuildingBlocks.Core.IdGenerator;
using Bms.BuildingBlocks.MultiTenant.Extensions;
using Bms.BuildingBlocks.Web.Extensions;
using Bms.BuildingBlocks.Web.Security;
using Bms.BuildingBlocks.Abstractions.Security;
using Bms.BuildingBlocks.Core.Context;
using Bms.System.Infrastructure;
using Bms.System.Infrastructure.Extensions;
using Bms.System.Infrastructure.SeedData;
using Bms.System.Infrastructure.Stores;
using Bms.System.Application.Extensions;
using Bms.System.Application.Mapping;
using Bms.System.Domain.Interfaces;
using Bms.System.Api.Converters;
using Bms.System.Api.Middleware;

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
// 配置核心服务
// ==========================================
// HttpContextAccessor (CurrentUser 需要)
builder.Services.AddHttpContextAccessor();

// CurrentUser 服务
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

Console.WriteLine("[Program] ICurrentUser and HttpContextAccessor registered");

builder.Services.AddMultiTenant<SqlTenantStore>();
builder.Services.AddSnowflakeIdGenerator(builder.Configuration);
builder.Services.AddMesAuthentication(builder.Configuration);
builder.Services.AddMesAuthorization();

Console.WriteLine("[Program] MultiTenant, SnowflakeIdGenerator, Authentication, Authorization registered");

// 配置 JSON 序列化：将 long 类型序列化为字符串，避免 JavaScript 大整数精度丢失
// 同时支持接收字符串格式的数字（前端发送时）
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // 注册自定义转换器
        options.JsonSerializerOptions.Converters.Add(new LongToStringConverter());
        options.JsonSerializerOptions.Converters.Add(new NullableLongToStringConverter());
        options.JsonSerializerOptions.Converters.Add(new IntToStringConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "BMS System API", Version = "v1" });
    // 使用完整类型名作为schema ID，避免冲突
    c.CustomSchemaIds(type => type.FullName?.Replace("+", "."));
    // 添加JWT认证到Swagger
    c.AddSecurityDefinition("Bearer", new()
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new()
    {
        {
            new()
            {
                Reference = new() { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// ==========================================
// 配置应用服务
// ==========================================
builder.Services.AddSystemServices(builder.Configuration);
builder.Services.AddApplicationServices();

// 配置 Mapster 对象映射
EntityMappingConfig.Configure();

var app = builder.Build();

// ==========================================
// 配置中间件管道
// ==========================================
// 优先配置Swagger，即使数据库初始化失败也能访问
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

// CORS中间件
app.UseCors("AllowFrontend");
Console.WriteLine("[Program] CORS middleware registered");

// 认证和授权中间件
app.UseAuthentication();
app.UseAuthorization();
Console.WriteLine("[Program] Authentication and Authorization middleware registered");

// 多租户中间件（必须在认证之后）
app.UseMultiTenant();
Console.WriteLine("[Program] MultiTenant middleware registered");

// 审计日志中间件（必须在认证授权和多租户之后）
app.UseAuditLog();
Console.WriteLine("[Program] AuditLog middleware registered");

// 路由映射
app.MapControllers();
Console.WriteLine("[Program] Controllers mapped");

// ==========================================
// 数据库初始化和种子数据
// ==========================================
// 在中间件管道配置完成后初始化数据库
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<SystemDbContext>();
        var idGenerator = scope.ServiceProvider.GetRequiredService<ISnowflakeIdGenerator>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        // 使用迁移而不是EnsureCreated，以保证版本控制
        logger.LogInformation("正在应用数据库迁移...");
        context.Database.Migrate();
        logger.LogInformation("数据库迁移应用成功。");

        // 初始化种子数据
        logger.LogInformation("正在初始化种子数据...");
        SystemSeedData.Initialize(context, idGenerator, passwordHasher);
        logger.LogInformation("种子数据初始化成功。");

        // 初始化系统配置（独立于用户数据，确保配置项存在）
        logger.LogInformation("正在初始化系统配置...");
        SystemSeedData.InitializeSystemConfigs(context);
        logger.LogInformation("系统配置初始化完成。");
    }
    catch (Exception ex)
    {
        // 数据库初始化失败时记录错误，但服务继续运行
        logger.LogError(ex, "出现错误：初始化数据库时发生异常，服务将在没有数据库的情况下继续运行。");
    }
}

app.Run();
