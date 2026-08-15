using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Bms.BuildingBlocks.Core.Extensions;
using Bms.BuildingBlocks.Core.IdGenerator;
using Bms.BuildingBlocks.MultiTenant.Abstractions;
using Bms.BuildingBlocks.MultiTenant.Extensions;
using Bms.BuildingBlocks.Storage.Extensions;
using Bms.BuildingBlocks.Web.Extensions;
using Bms.BuildingBlocks.Web.Security;
using Bms.BuildingBlocks.Abstractions.Security;
using Bms.BuildingBlocks.Core.Context;
using Bms.BuildingBlocks.Web.Converters;
using Bms.Store.Infrastructure;
using Bms.Store.Infrastructure.Extensions;
using Bms.Store.Infrastructure.Stores;
using Bms.Store.Application.Extensions;
using Bms.Store.Application.Mapping;
using Bms.Store.Application.SeedData;
using Bms.Store.Application.Services;
using Bms.Store.Api.BackgroundServices;
using Bms.Store.Api.Filters;

// 启用遗留时间戳行为
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 配置功能开关
// ==========================================
var useHttpsRedirection = builder.Configuration.GetValue<bool>("FeatureFlags:UseHttpsRedirection");

// ==========================================
// 配置CORS
// ==========================================
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
            policy.AllowAnyOrigin()
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
    });
});

// ==========================================
// 配置核心服务
// ==========================================
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

// 内存缓存（UserStatusCheckMiddleware 需要，L2 修复）
builder.Services.AddMemoryCache();

// 注册 SystemTenantStore，从 bms_system 数据库读取租户信息
var systemDbConnectionString = builder.Configuration.GetConnectionString("SystemDb");
builder.Services.AddScoped<ITenantStore>(sp => new SystemTenantStore(systemDbConnectionString!));

// 手动注册多租户服务（避免使用泛型方法实例化需要参数的 TenantStore）
builder.Services.AddScoped<ITenantProvider, Bms.BuildingBlocks.MultiTenant.Services.DefaultTenantProvider>();
builder.Services.AddScoped<ITenantResolutionStrategy, Bms.BuildingBlocks.MultiTenant.Resolvers.ClaimTenantResolver>();
builder.Services.AddScoped<ITenantResolutionStrategy, Bms.BuildingBlocks.MultiTenant.Resolvers.HeaderTenantResolver>();
builder.Services.AddScoped<ITenantResolutionStrategy, Bms.BuildingBlocks.MultiTenant.Resolvers.HostTenantResolver>();

builder.Services.AddSnowflakeIdGenerator(builder.Configuration);
builder.Services.AddObjectStorage(builder.Configuration);
builder.Services.AddMesAuthentication(builder.Configuration);
builder.Services.AddMesAuthorization();

// 配置 JSON 序列化：将 long 类型序列化为字符串，避免 JavaScript 大整数精度丢失
// 同时支持接收字符串格式的数字（前端发送时）
// 注册全局 StoreRequiredActionFilter：未携带 X-Store-Id 的业务 API 请求直接拒绝（纵深防御）
builder.Services.AddControllers(opts =>
    {
        opts.Filters.Add<StoreRequiredActionFilter>();
    })
    .AddJsonOptions(options =>
    {
        // 注册自定义转换器
        options.JsonSerializerOptions.Converters.Add(new LongToStringConverter());
        options.JsonSerializerOptions.Converters.Add(new NullableLongToStringConverter());
        options.JsonSerializerOptions.Converters.Add(new IntToStringConverter());
        options.JsonSerializerOptions.Converters.Add(new NullableIntToStringConverter());
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "BMS Store API", Version = "v1" });
    c.CustomSchemaIds(type => type.FullName?.Replace("+", "."));
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
builder.Services.AddStoreServices(builder.Configuration);
builder.Services.AddApplicationServices();

// 跨服务用户查询（直查 bms_system.Users 表，供门店授权分配使用）
builder.Services.AddScoped<Bms.Store.Application.Abstractions.IUserQueryService, Bms.Store.Api.Services.UserQueryService>();

EntityMappingConfig.Configure();

// ==========================================
// 配置 HttpClient for System API
// ==========================================
builder.Services.AddHttpClient("SystemApi", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

// 注册菜单自注册服务（后台运行）
builder.Services.AddHostedService<StoreMenuRegistrationService>();

// 注册预约爽约自动判断定时任务（后台运行,每5分钟扫描）
builder.Services.AddHostedService<AppointmentNoShowService>();

// 注册日结自动汇总兜底定时任务（后台运行,每日02:00执行）
builder.Services.AddHostedService<DailySettlementSummaryService>();

// 注册月度统计自动聚合定时任务（后台运行,每月1日02:30执行）（P-STAT-01）
builder.Services.AddHostedService<MonthlyStatAggregateService>();

// 注册库存预警自动扫描定时任务（后台运行,每日03:00执行）
builder.Services.AddHostedService<InventoryAlertScanService>();

// 注册设备保养提醒自动生成定时任务（后台运行,每日06:00执行，P-EQP-03 提醒生成逻辑）
builder.Services.AddHostedService<EquipmentMaintenanceReminderService>();

// 注册积分过期清零定时任务（后台运行,每日01:00执行，P-PTS-04 第一阶段）
builder.Services.AddHostedService<PointsExpiryService>();

// 注册客户生日提醒站内信自动发送定时任务（后台运行,每日07:00执行）
builder.Services.AddHostedService<BirthdayReminderService>();

// 注册孤儿对象清理定时任务（后台运行,每日04:00执行，图片对象存储改造阶段4）
builder.Services.AddHostedService<OrphanObjectCleanupService>();

var app = builder.Build();

// ==========================================
// 配置中间件管道
// ==========================================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (useHttpsRedirection)
{
    app.UseHttpsRedirection();
}

app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.UseMultiTenant();

// 用户状态校验中间件（L2 修复：在多租户之后检查用户是否被禁用）
app.UseMiddleware<Bms.Store.Api.Middleware.UserStatusCheckMiddleware>();

// 门店上下文解析中间件（解析 StoreCode 并写入 HttpContext.Items，供 ICurrentUser.StoreCode 读取）
app.UseMiddleware<Bms.Store.Api.Middleware.StoreContextMiddleware>();

// 门店访问权限校验中间件（M2 修复：校验 X-Store-Id 是否在用户授权范围内）
app.UseMiddleware<Bms.Store.Api.Middleware.StoreAccessMiddleware>();

// 权限码校验中间件（M8 修复：通过 UI 配置的菜单按钮权限码校验，替代业务层硬编码角色判断）
app.UseMiddleware<Bms.Store.Api.Middleware.StorePermissionMiddleware>();

app.MapControllers();

// ==========================================
// 数据库初始化
// ==========================================
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<StoreDbContext>();
        var idGenerator = scope.ServiceProvider.GetRequiredService<ISnowflakeIdGenerator>();

        logger.LogInformation("正在应用数据库迁移...");
        context.Database.Migrate();
        logger.LogInformation("数据库迁移应用成功。");

        // 测试种子数据加载开关（默认关闭，仅测试/开发环境启用）
        // 启用后从 JSON 资源加载业务数据（Stores、UserStores），覆盖现有数据
        // 菜单数据由 StoreMenuRegistrationService 通过 System API 自动注册，不在本加载器处理范围
        var seedTestDataEnabled = builder.Configuration.GetValue<bool>("SeedTestData:Enabled");
        if (seedTestDataEnabled)
        {
            logger.LogWarning("检测到 SeedTestData:Enabled=true，加载 Store 测试种子数据（将覆盖现有业务数据）...");
            await TestSeedData.InitializeAsync(context, logger);
        }

        logger.LogInformation("Store 服务数据库初始化完成。");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "数据库初始化失败，服务将在没有数据库的情况下继续运行。");
    }
}

app.Run();
